using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using StarTickets.Models;
using StarTickets.Models.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QRCoder;

namespace StarTickets.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        // NEW: Send ticket confirmation email with e-tickets attached
        public async Task SendTicketConfirmationEmailAsync(Booking booking)
        {
            try
            {
                var subject = $"Your Tickets for {booking.Event.EventName} - Booking #{booking.BookingReference}";
                var body = GenerateTicketConfirmationEmailBody(booking);

                // Generate PDF attachments for each ticket
                var attachments = new List<Attachment>();

                foreach (var detail in booking.BookingDetails)
                {
                    foreach (var ticket in detail.Tickets)
                    {
                        var pdfBytes = GenerateTicketPdf(ticket, booking);
                        var attachment = new Attachment(
                            new MemoryStream(pdfBytes),
                            $"Ticket_{ticket.TicketNumber}.pdf",
                            "application/pdf"
                        );
                        attachments.Add(attachment);
                    }
                }

                await SendEmailWithAttachmentsAsync(booking.Customer.Email, subject, body, attachments);

                // Dispose attachments
                foreach (var attachment in attachments)
                {
                    attachment.Dispose();
                }

                _logger.LogInformation($"Ticket confirmation email sent successfully to {booking.Customer.Email} for booking {booking.BookingReference}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send ticket confirmation email for booking {booking.BookingReference}");
            }
        }

        // NEW: Send a single ticket email (by ticket)
        public async Task SendTicketEmailAsync(Ticket ticket, Booking booking)
        {
            try
            {
                var subject = $"Your Ticket {ticket.TicketNumber} for {booking.Event.EventName}";
                var body = GenerateSingleTicketEmailBody(booking, ticket);

                // Generate single PDF attachment
                var attachments = new List<Attachment>();
                var pdfBytes = GenerateTicketPdf(ticket, booking);
                var attachment = new Attachment(new MemoryStream(pdfBytes), $"Ticket_{ticket.TicketNumber}.pdf", "application/pdf");
                attachments.Add(attachment);

                await SendEmailWithAttachmentsAsync(booking.Customer.Email, subject, body, attachments);

                foreach (var a in attachments) a.Dispose();

                _logger.LogInformation($"Single ticket email sent to {booking.Customer.Email} for ticket {ticket.TicketNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send single ticket email for ticket {ticket.TicketNumber}");
            }
        }

        // NEW: Generate individual ticket PDF
        private byte[] GenerateTicketPdf(Ticket ticket, Booking booking)
        {
            var bookingDetail = booking.BookingDetails.First(bd => bd.Tickets.Contains(ticket));

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);

                    // Header
                    page.Header().Height(100).Background(Colors.Blue.Medium).AlignCenter().AlignMiddle().Column(col =>
                    {
                        col.Item().Text("🎟️ STARTICKETS")
                            .FontSize(24).Bold().FontColor(Colors.White);
                        col.Item().Text("YOUR EVENT TICKET")
                            .FontSize(12).FontColor(Colors.White);
                    });

                    // Main Content
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        // Event Details Section
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(20).Column(eventCol =>
                        {
                            eventCol.Item().Text(booking.Event.EventName)
                                .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);

                            eventCol.Item().PaddingTop(10).Text($"📅 {booking.Event.EventDate:dddd, MMMM dd, yyyy}")
                                .FontSize(14);

                            eventCol.Item().Text($"🕐 {booking.Event.EventDate:h:mm tt}")
                                .FontSize(14);

                            eventCol.Item().Text($"📍 {booking.Event.Venue?.VenueName ?? "TBA"}")
                                .FontSize(14);

                            eventCol.Item().Text($"🎫 {bookingDetail.TicketCategory.CategoryName}")
                                .FontSize(14).Bold();
                        });

                        // Spacer
                        col.Item().PaddingVertical(10);

                        // Ticket Details Section
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(20).Row(row =>
                        {
                            // Left Column - Ticket Info
                            row.RelativeItem(2).Column(ticketCol =>
                            {
                                ticketCol.Item().Text("TICKET DETAILS").FontSize(14).Bold();
                                ticketCol.Item().PaddingTop(10).Text($"Ticket Number: {ticket.TicketNumber}")
                                    .FontSize(12).FontFamily("Courier New");
                                ticketCol.Item().Text($"Booking Reference: {booking.BookingReference}")
                                    .FontSize(12);
                                ticketCol.Item().Text($"Price: ${bookingDetail.UnitPrice:F2}")
                                    .FontSize(12);
                                ticketCol.Item().Text($"Status: {(ticket.IsUsed ? "Used" : "Valid")}")
                                    .FontSize(12).Bold()
                                    .FontColor(ticket.IsUsed ? Colors.Red.Medium : Colors.Green.Medium);
                            });

                            // Right Column - QR Code
                            row.RelativeItem(1).AlignCenter().Column(qrCol =>
                            {
                                qrCol.Item().Text("SCAN TO VALIDATE").FontSize(10).Bold();

                                // Generate QR Code
                                try
                                {
                                    using var qrGenerator = new QRCodeGenerator();
                                    using var qrCodeData = qrGenerator.CreateQrCode(ticket.QRCode, QRCodeGenerator.ECCLevel.Q);
                                    var qrCode = new PngByteQRCode(qrCodeData);
                                    byte[] qrCodeBytes = qrCode.GetGraphic(20);

                                    qrCol.Item().PaddingTop(5).Image(qrCodeBytes).FitArea();
                                }
                                catch (Exception ex)
                                {
                                    qrCol.Item().PaddingTop(5).Border(1).Height(100).Width(100)
                                        .AlignCenter().AlignMiddle().Text("QR Code\nGeneration\nFailed")
                                        .FontSize(8);
                                }

                                qrCol.Item().PaddingTop(5).Text(ticket.QRCode)
                                    .FontSize(8).FontFamily("Courier New");
                            });
                        });

                        // Important Notes
                        col.Item().PaddingTop(20).Border(1).BorderColor(Colors.Orange.Medium)
                            .Background(Colors.Orange.Lighten4).Padding(15).Column(notesCol =>
                            {
                                notesCol.Item().Text("⚠️ IMPORTANT INSTRUCTIONS").FontSize(12).Bold();
                                notesCol.Item().PaddingTop(5).Text("• Present this ticket (digital or printed) at the venue entrance");
                                notesCol.Item().Text("• Arrive at least 30 minutes before the event start time");
                                notesCol.Item().Text("• Bring a valid photo ID for verification");
                                notesCol.Item().Text("• This ticket is non-transferable and non-refundable");
                                notesCol.Item().Text("• Screenshots or photos of this ticket are not valid");
                            });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC • StarTickets © 2024")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            return pdf.GeneratePdf();
        }

        // NEW: Send email with attachments
        private async Task SendEmailWithAttachmentsAsync(string to, string subject, string body, List<Attachment> attachments)
        {
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
            client.EnableSsl = _emailSettings.EnableSsl;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);

            // Add attachments
            foreach (var attachment in attachments)
            {
                mailMessage.Attachments.Add(attachment);
            }

            await client.SendMailAsync(mailMessage);
        }

        // NEW: Generate ticket confirmation email body
        private string GenerateTicketConfirmationEmailBody(Booking booking)
        {
            var ticketCount = booking.BookingDetails.Sum(bd => bd.Quantity);
            var ticketWord = ticketCount == 1 ? "ticket" : "tickets";

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .event-card {{ background: white; border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px; margin: 20px 0; }}
                        .ticket-summary {{ background: #e8f4f8; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0; }}
                        .important-info {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .btn {{ display: inline-block; padding: 12px 25px; background: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 10px 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎉 Payment Successful!</h1>
                            <p>Your {ticketWord} for {booking.Event.EventName}</p>
                        </div>
                        <div class='content'>
                            <h2>Hello {booking.Customer.FirstName},</h2>
                            <p>Great news! Your payment has been processed successfully and your e-{ticketWord} {(ticketCount == 1 ? "is" : "are")} attached to this email.</p>
                            
                            <div class='event-card'>
                                <h3>📅 Event Details</h3>
                                <p><strong>{booking.Event.EventName}</strong></p>
                                <p>📍 <strong>Venue:</strong> {booking.Event.Venue?.VenueName ?? "TBA"}</p>
                                <p>📅 <strong>Date:</strong> {booking.Event.EventDate:dddd, MMMM dd, yyyy}</p>
                                <p>🕐 <strong>Time:</strong> {booking.Event.EventDate:h:mm tt}</p>
                                <p>🏷️ <strong>Category:</strong> {booking.Event.Category?.CategoryName}</p>
                            </div>

                            <div class='ticket-summary'>
                                <h3>🎫 Booking Summary</h3>
                                <p><strong>Booking Reference:</strong> {booking.BookingReference}</p>
                                <p><strong>Payment Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</p>
                                <p><strong>Total Tickets:</strong> {ticketCount}</p>
                                
                                <h4>Ticket Breakdown:</h4>
                                <ul>
                                    {string.Join("", booking.BookingDetails.Select(bd =>
                                        $"<li>{bd.TicketCategory.CategoryName}: {bd.Quantity} × ${bd.UnitPrice:F2} = ${bd.TotalPrice:F2}</li>"
                                    ))}
                                </ul>
                                
                                {(booking.DiscountAmount > 0 ?
                                    $"<p><strong>Subtotal:</strong> ${booking.TotalAmount:F2}</p>" +
                                    $"<p><strong>Discount ({booking.PromoCodeUsed}):</strong> -${booking.DiscountAmount:F2}</p>" : "")}
                                
                                <p><strong>Final Amount Paid:</strong> ${booking.FinalAmount:F2}</p>
                            </div>

                            <div class='important-info'>
                                <h3>📱 How to Use Your E-Tickets</h3>
                                <ul>
                                    <li><strong>Mobile:</strong> Save the PDF attachments to your phone for easy access</li>
                                    <li><strong>Print:</strong> You can also print your tickets as backup</li>
                                    <li><strong>Entry:</strong> Present either the digital or printed ticket at the venue</li>
                                    <li><strong>ID Required:</strong> Bring a valid photo ID for verification</li>
                                    <li><strong>Arrival:</strong> Please arrive 30 minutes before the event start time</li>
                                </ul>
                            </div>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='#' class='btn'>📱 Download StarTickets App</a>
                                <a href='#' class='btn'>🎫 Manage My Bookings</a>
                            </div>

                            <h3>❓ Need Help?</h3>
                            <p>If you have any questions about your booking or need assistance:</p>
                            <ul>
                                <li>📧 Email: support@startickets.com</li>
                                <li>📞 Phone: 1-800-STAR-TIX</li>
                                <li>💬 Live Chat: Available on our website</li>
                            </ul>
                            
                            <p>Thank you for choosing StarTickets! We hope you have an amazing time at the event.</p>
                            
                            <p>Best regards,<br>The StarTickets Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2024 StarTickets. All rights reserved.</p>
                            <p>This email was sent to {booking.Customer.Email}</p>
                            <p>Booking Reference: {booking.BookingReference}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        // NEW: Generate single ticket email body
        private string GenerateSingleTicketEmailBody(Booking booking, Ticket ticket)
        {
            var bookingDetail = booking.BookingDetails.First(bd => bd.Tickets.Contains(ticket));
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #2563eb 0%, #0ea5e9 100%); color: white; padding: 24px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 24px; border-radius: 0 0 10px 10px; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .card {{ background: white; border: 1px solid #eee; border-radius: 8px; padding: 16px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Your Ticket Is Attached</h2>
                            <p>{booking.Event.EventName}</p>
                        </div>
                        <div class='content'>
                            <p>Hello {booking.Customer.FirstName},</p>
                            <p>Your ticket is attached to this email as a PDF.</p>
                            <div class='card'>
                                <p><strong>Ticket Number:</strong> {ticket.TicketNumber}</p>
                                <p><strong>Booking Ref:</strong> {booking.BookingReference}</p>
                                <p><strong>Category:</strong> {bookingDetail.TicketCategory.CategoryName}</p>
                                <p><strong>Date:</strong> {booking.Event.EventDate:dddd, MMMM dd, yyyy h:mm tt}</p>
                                <p><strong>Venue:</strong> {booking.Event.Venue?.VenueName ?? "TBA"}</p>
                            </div>
                            <p>Present this ticket at the venue for entry.</p>
                        </div>
                        <div class='footer'>
                            <p>StarTickets © {DateTime.UtcNow:yyyy}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        public async Task SendWelcomeEmailAsync(User user)
        {
            var subject = "Welcome to StarTickets!";
            var roleTitle = user.Role switch
            {
                2 => "Event Organizer",
                3 => "Customer",
                _ => "User"
            };

            var body = GenerateWelcomeEmailBody(user, roleTitle);
            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(User user, string resetUrl)
        {
            var subject = "Reset Your StarTickets Password";
            var body = GeneratePasswordResetEmailBody(user, resetUrl);
            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendPasswordResetConfirmationEmailAsync(User user)
        {
            var subject = "Password Reset Successful - StarTickets";
            var body = GeneratePasswordResetConfirmationEmailBody(user);
            await SendEmailAsync(user.Email, subject, body);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
            }
        }

        private string GenerateWelcomeEmailBody(User user, string roleTitle)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .btn {{ display: inline-block; padding: 12px 25px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎟️ Welcome to StarTickets!</h1>
                            <p>Your gateway to amazing events</p>
                        </div>
                        <div class='content'>
                            <h2>Hello {user.FirstName} {user.LastName}!</h2>
                            <p>Congratulations! Your StarTickets account has been successfully created.</p>
                            
                            <p><strong>Account Details:</strong></p>
                            <ul>
                                <li>Email: {user.Email}</li>
                                <li>Account Type: {roleTitle}</li>
                                <li>Registration Date: {DateTime.Now:MMMM dd, yyyy}</li>
                            </ul>

                            {(user.Role == 2 ?
                                @"<p>As an <strong>Event Organizer</strong>, you can now:</p>
                                <ul>
                                    <li>Create and manage your events</li>
                                    <li>Track ticket sales and revenue</li>
                                    <li>Communicate with attendees</li>
                                    <li>Access detailed analytics</li>
                                </ul>" :
                                @"<p>As a <strong>Customer</strong>, you can now:</p>
                                <ul>
                                    <li>Discover exciting events</li>
                                    <li>Book tickets instantly</li>
                                    <li>Manage your bookings</li>
                                    <li>Get event updates</li>
                                </ul>")}

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='#' class='btn'>Get Started</a>
                            </div>

                            <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                            
                            <p>Thank you for joining StarTickets!</p>
                            <p>The StarTickets Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2024 StarTickets. All rights reserved.</p>
                            <p>This email was sent to {user.Email}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GeneratePasswordResetEmailBody(User user, string resetUrl)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .btn {{ display: inline-block; padding: 15px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold; }}
                        .btn:hover {{ background: #5a67d8; }}
                        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                        .security-info {{ background: #e8f4f8; padding: 15px; border-left: 4px solid #667eea; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🔒 Password Reset Request</h1>
                            <p>StarTickets Account Security</p>
                        </div>
                        <div class='content'>
                            <h2>Hello {user.FirstName},</h2>
                            <p>We received a request to reset the password for your StarTickets account associated with <strong>{user.Email}</strong>.</p>
                            
                            <div class='security-info'>
                                <h3>🔐 Security Information</h3>
                                <ul>
                                    <li>Request Time: {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm} UTC</li>
                                    <li>This link will expire in <strong>10 Minutes</strong></li>
                                    <li>For security, this link can only be used once</li>
                                </ul>
                            </div>

                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetUrl}' class='btn'>Reset Your Password</a>
                            </div>

                            <div class='warning'>
                                <strong>⚠️ Important:</strong>
                                <ul>
                                    <li>If you didn't request this password reset, please ignore this email</li>
                                    <li>Never share this reset link with anyone</li>
                                    <li>Our support team will never ask for your password</li>
                                </ul>
                            </div>

                            <p>If the button above doesn't work, you can copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; background: #f1f1f1; padding: 10px; border-radius: 5px; font-family: monospace;'>{resetUrl}</p>
                            
                            <p>If you have any questions or concerns, please contact our support team.</p>
                            
                            <p>Best regards,<br>The StarTickets Security Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2024 StarTickets. All rights reserved.</p>
                            <p>This email was sent to {user.Email}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GeneratePasswordResetConfirmationEmailBody(User user)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .success {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; color: #155724; }}
                        .security-tips {{ background: #e8f4f8; padding: 15px; border-left: 4px solid #28a745; margin: 20px 0; }}
                        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>✅ Password Reset Successful</h1>
                            <p>Your StarTickets account is secure</p>
                        </div>
                        <div class='content'>
                            <h2>Hello {user.FirstName},</h2>
                            
                            <div class='success'>
                                <strong>✅ Success!</strong> Your password has been successfully reset for your StarTickets account.
                            </div>
                            
                            <p><strong>Account Details:</strong></p>
                            <ul>
                                <li>Email: {user.Email}</li>
                                <li>Password Reset: {DateTime.UtcNow:MMMM dd, yyyy 'at' HH:mm} UTC</li>
                            </ul>

                            <div class='security-tips'>
                                <h3>🔐 Security Tips</h3>
                                <ul>
                                    <li>Use a strong, unique password for your account</li>
                                    <li>Never share your password with anyone</li>
                                    <li>Log out from shared or public computers</li>
                                    <li>Contact support if you notice any suspicious activity</li>
                                </ul>
                            </div>

                            <p><strong>What's Next?</strong></p>
                            <p>You can now log in to your StarTickets account using your new password. All previous reset tokens have been invalidated for security.</p>

                            <p><strong>Didn't reset your password?</strong></p>
                            <p>If you didn't initiate this password reset, please contact our support team immediately at support@startickets.com</p>
                            
                            <p>Thank you for keeping your account secure!</p>
                            <p>The StarTickets Team</p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2024 StarTickets. All rights reserved.</p>
                            <p>This email was sent to {user.Email}</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}