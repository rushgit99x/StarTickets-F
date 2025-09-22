class BookingConfirmation {
    constructor(options = {}) {
        this.hasPaymentPending = options.hasPaymentPending || false;
        this.bookingId = options.bookingId;
        this.finalAmount = options.finalAmount;
        this.tickets = options.tickets || [];
        this.downloadTicketUrl = options.downloadTicketUrl;
        this.emailTicketsUrl = options.emailTicketsUrl;
        this.homeUrl = options.homeUrl;

        // Ticket design options
        this.ticketDesign = {
            width: 600,
            height: 300,
            backgroundColor: '#ffffff',
            primaryColor: '#2563eb',
            secondaryColor: '#64748b',
            accentColor: '#0ea5e9',
            borderRadius: 12,
            padding: 20,
            qrSize: 120
        };

        this.timeLeft = 15 * 60; // 15 minutes in seconds
        this.countdownTimer = null;
        this.ticketImages = new Map();

        this.init();
    }

    init() {
        if (this.hasPaymentPending) {
            this.initPaymentMode();
        } else {
            this.initCompletedMode();
        }
    }

    initPaymentMode() {
        this.startCountdown();
        this.setupCardFormatting();
        this.setupFormValidation();
    }

    initCompletedMode() {
        // Get fresh ticket data with server-generated QR codes
        this.fetchTicketDataAndGenerate();
    }

    // Fetch ticket data from server with QR codes
    async fetchTicketDataAndGenerate() {
        try {
            const response = await fetch(`/Booking/GetTicketDataWithQR?bookingId=${this.bookingId}`);

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const serverTickets = await response.json();

            // Update tickets array with server data including QR codes
            this.tickets = serverTickets;

            // Generate ticket images with the QR codes from server
            await this.generateTicketImages();

        } catch (error) {
            console.error('Error fetching ticket data:', error);
            this.showFallbackTickets();
        }
    }

    // Enhanced countdown timer for payment
    startCountdown() {
        const countdownElement = document.getElementById('countdown');
        if (!countdownElement) return;

        this.countdownTimer = setInterval(() => {
            const minutes = Math.floor(this.timeLeft / 60);
            const seconds = this.timeLeft % 60;
            countdownElement.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

            if (this.timeLeft <= 0) {
                this.handleSessionExpired();
                return;
            }
            this.timeLeft--;
        }, 1000);
    }

    handleSessionExpired() {
        clearInterval(this.countdownTimer);

        const paymentSection = document.getElementById('paymentSection');
        if (paymentSection) {
            paymentSection.innerHTML = `
                <div class="alert alert-danger">
                    <h4><i class="fas fa-clock me-2"></i>Payment Time Expired</h4>
                    <p>Your booking has expired. Please start a new booking to secure your tickets.</p>
                    <a href="${this.homeUrl}" class="btn btn-primary">
                        <i class="fas fa-home me-2"></i>Browse Events
                    </a>
                </div>
            `;
        } else {
            alert('Session expired! Please restart the booking process.');
            window.location.href = this.homeUrl;
        }
    }

    // Enhanced card input formatting
    setupCardFormatting() {
        const cardNumberInput = document.getElementById('cardNumber');
        const expiryDateInput = document.getElementById('expiryDate');
        const cvvInput = document.getElementById('cvv');

        if (cardNumberInput) {
            cardNumberInput.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                let formattedValue = value.replace(/(\d{4})(?=\d)/g, '$1 ');
                if (formattedValue.length > 19) formattedValue = formattedValue.substring(0, 19);
                e.target.value = formattedValue;
            });
        }

        if (expiryDateInput) {
            expiryDateInput.addEventListener('input', (e) => {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length >= 2) {
                    value = value.substring(0, 2) + '/' + value.substring(2, 4);
                }
                e.target.value = value;
            });
        }

        if (cvvInput) {
            cvvInput.addEventListener('input', (e) => {
                e.target.value = e.target.value.replace(/\D/g, '');
            });
        }
    }

    // Enhanced form validation and submission
    setupFormValidation() {
        const paymentForm = document.getElementById('paymentForm');
        if (!paymentForm) return;

        paymentForm.addEventListener('submit', (e) => {
            // If invalid, prevent submission and show validation
            if (!this.validatePaymentForm(paymentForm)) {
                e.preventDefault();
                return;
            }
            // Otherwise allow native form submit so browser follows redirect
        });
    }

    validatePaymentForm(form) {
        const cardNumber = document.getElementById('cardNumber').value.replace(/\s/g, '');
        const expiryDate = document.getElementById('expiryDate').value;
        const cvv = document.getElementById('cvv').value;
        const termsCheck = document.getElementById('termsCheck').checked;

        // Reset validation states
        form.classList.remove('was-validated');

        let isValid = true;

        // Validate card number (basic Luhn algorithm)
        if (!this.isValidCardNumber(cardNumber)) {
            document.getElementById('cardNumber').setCustomValidity('Invalid card number');
            isValid = false;
        } else {
            document.getElementById('cardNumber').setCustomValidity('');
        }

        // Validate expiry date
        if (!this.isValidExpiryDate(expiryDate)) {
            document.getElementById('expiryDate').setCustomValidity('Card has expired or invalid date');
            isValid = false;
        } else {
            document.getElementById('expiryDate').setCustomValidity('');
        }

        // Validate CVV
        if (cvv.length < 3 || cvv.length > 4) {
            document.getElementById('cvv').setCustomValidity('Invalid CVV');
            isValid = false;
        } else {
            document.getElementById('cvv').setCustomValidity('');
        }

        // Check terms
        if (!termsCheck) {
            isValid = false;
        }

        form.classList.add('was-validated');

        return isValid && form.checkValidity();
    }

    async processPayment(form) {
        const submitButton = form.querySelector('button[type="submit"]');
        const buttonText = submitButton.querySelector('.btn-text');
        const loadingSpinner = submitButton.querySelector('.loading-spinner');

        // Show loading state
        if (buttonText) buttonText.style.display = 'none';
        if (loadingSpinner) loadingSpinner.style.display = 'inline-flex';
        submitButton.disabled = true;

        try {
            const formData = new FormData(form);

            const response = await fetch(form.action, {
                method: 'POST',
                body: formData
            });

            // If server sent a redirect (e.g., to BookingConfirmation with TempData), follow it
            if (response.redirected) {
                window.location.href = response.url;
                return;
            }

            if (response.ok) {
                // Fallback: reload current page
                window.location.reload();
            } else {
                throw new Error('Payment failed');
            }
        } catch (error) {
            console.error('Payment error:', error);

            // Show error state
            if (buttonText) buttonText.style.display = 'inline-flex';
            if (loadingSpinner) loadingSpinner.style.display = 'none';
            submitButton.disabled = false;

            this.showPaymentError('Payment failed. Please check your card details and try again.');
        }
    }

    showPaymentError(message) {
        const errorContainer = document.getElementById('payment-errors') || document.createElement('div');
        errorContainer.id = 'payment-errors';
        errorContainer.className = 'alert alert-danger mt-3';
        errorContainer.innerHTML = `
            <i class="fas fa-exclamation-triangle me-2"></i>
            ${message}
        `;

        const form = document.getElementById('paymentForm');
        if (form && !document.getElementById('payment-errors')) {
            form.parentNode.insertBefore(errorContainer, form.nextSibling);
        }
    }

    // Card validation functions
    isValidCardNumber(cardNumber) {
        // Allow known test-decline card to bypass Luhn so server can handle failure
        if (cardNumber === '4000000000000002') return true;
        // Basic Luhn algorithm check
        if (cardNumber.length < 13 || cardNumber.length > 19) return false;

        let sum = 0;
        let isEven = false;

        for (let i = cardNumber.length - 1; i >= 0; i--) {
            let digit = parseInt(cardNumber[i]);

            if (isEven) {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }

            sum += digit;
            isEven = !isEven;
        }

        return sum % 10 === 0;
    }

    isValidExpiryDate(expiryDate) {
        const regex = /^(0[1-9]|1[0-2])\/\d{2}$/;
        if (!regex.test(expiryDate)) return false;

        const [month, year] = expiryDate.split('/');
        const expiry = new Date(2000 + parseInt(year), parseInt(month) - 1);
        const now = new Date();

        return expiry > now;
    }

    // Enhanced ticket image generation with server-side QR codes
    async generateTicketImages() {
        if (!this.tickets.length) return;

        for (let index = 0; index < this.tickets.length; index++) {
            const ticket = this.tickets[index];
            const elementId = `ticket-image-${index + 1}`;
            await this.generateTicketImage(elementId, ticket, index + 1);
        }
    }

    async generateTicketImage(elementId, ticketData, ticketNumber) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.error(`Element with ID ${elementId} not found`);
            return;
        }

        try {
            // Create canvas for the ticket
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            const design = this.ticketDesign;

            canvas.width = design.width;
            canvas.height = design.height;

            // Create gradient background
            const gradient = ctx.createLinearGradient(0, 0, design.width, design.height);
            gradient.addColorStop(0, design.backgroundColor);
            gradient.addColorStop(1, '#f8fafc');

            // Draw background with rounded corners
            this.drawRoundedRect(ctx, 0, 0, design.width, design.height, design.borderRadius, gradient);

            // Draw header stripe
            const headerGradient = ctx.createLinearGradient(0, 0, design.width, 0);
            headerGradient.addColorStop(0, design.primaryColor);
            headerGradient.addColorStop(1, design.accentColor);
            this.drawRoundedRect(ctx, 0, 0, design.width, 60, design.borderRadius, headerGradient, true);

            // Add ticket title
            ctx.fillStyle = '#ffffff';
            ctx.font = 'bold 24px Arial, sans-serif';
            ctx.textAlign = 'left';
            ctx.fillText('STARTICKETS', design.padding, 35);

            // Add ticket number
            ctx.font = '14px Arial, sans-serif';
            ctx.textAlign = 'right';
            ctx.fillText(`Ticket #${ticketNumber}`, design.width - design.padding, 35);

            // Add ticket details first (left side)
            this.addTicketDetails(ctx, ticketData, design);

            // Use server-generated QR code if available
            if (ticketData.qrCodeDataUrl) {
                const qrImage = new Image();
                qrImage.onload = () => {
                    // Draw QR code on the right side
                    const qrX = design.width - design.qrSize - design.padding;
                    const qrY = 80;

                    // Add QR code background
                    ctx.fillStyle = '#ffffff';
                    ctx.fillRect(qrX - 10, qrY - 10, design.qrSize + 20, design.qrSize + 20);
                    ctx.strokeStyle = '#e2e8f0';
                    ctx.strokeRect(qrX - 10, qrY - 10, design.qrSize + 20, design.qrSize + 20);

                    // Draw QR code
                    ctx.drawImage(qrImage, qrX, qrY, design.qrSize, design.qrSize);

                    // Convert canvas to image and display
                    this.displayTicketImage(element, canvas, ticketData, ticketNumber);
                };

                qrImage.onerror = () => {
                    console.error('Failed to load server-generated QR code image');
                    // Generate ticket without QR code
                    this.addNoQRPlaceholder(ctx, design);
                    this.displayTicketImage(element, canvas, ticketData, ticketNumber);
                };

                qrImage.src = ticketData.qrCodeDataUrl;
            } else {
                // Generate ticket without QR code
                this.addNoQRPlaceholder(ctx, design);
                this.displayTicketImage(element, canvas, ticketData, ticketNumber);
            }

        } catch (error) {
            console.error('Error generating ticket image:', error);
            this.displayErrorMessage(element, `Error generating ticket: ${error.message}`);
        }
    }

    addNoQRPlaceholder(ctx, design) {
        const qrX = design.width - design.qrSize - design.padding;
        const qrY = 80;

        // Add placeholder background
        ctx.fillStyle = '#f3f4f6';
        ctx.fillRect(qrX, qrY, design.qrSize, design.qrSize);
        ctx.strokeStyle = '#d1d5db';
        ctx.strokeRect(qrX, qrY, design.qrSize, design.qrSize);

        // Add placeholder text
        ctx.fillStyle = '#6b7280';
        ctx.font = '14px Arial, sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('QR CODE', qrX + design.qrSize / 2, qrY + design.qrSize / 2 - 10);
        ctx.fillText('UNAVAILABLE', qrX + design.qrSize / 2, qrY + design.qrSize / 2 + 10);
    }

    displayTicketImage(element, canvas, ticketData, ticketNumber) {
        const ticketImage = document.createElement('img');
        ticketImage.src = canvas.toDataURL('image/png');
        ticketImage.className = 'ticket-image img-fluid';
        ticketImage.style.maxWidth = '100%';
        ticketImage.style.height = 'auto';
        ticketImage.style.border = '1px solid #e2e8f0';
        ticketImage.style.borderRadius = '12px';
        ticketImage.style.boxShadow = '0 4px 6px -1px rgba(0, 0, 0, 0.1)';

        // Clear element and add image
        element.innerHTML = '';
        element.appendChild(ticketImage);

        // Add download buttons below the image
        this.addDownloadButtons(element, ticketData, canvas, ticketNumber);

        // Store canvas for later use
        this.ticketImages.set(ticketNumber, canvas);
    }

    displayErrorMessage(element, message) {
        element.innerHTML = `<div class="alert alert-warning text-center">
            <i class="fas fa-exclamation-triangle me-2"></i>
            ${message}
            <br><small class="text-muted">Please refresh the page to try again</small>
        </div>`;
    }

    addTicketDetails(ctx, ticketData, design) {
        const leftX = design.padding;
        let currentY = 90;
        const lineHeight = 25;

        // Event name
        ctx.fillStyle = design.primaryColor;
        ctx.font = 'bold 20px Arial, sans-serif';
        ctx.textAlign = 'left';
        ctx.fillText(ticketData.eventName || 'Event Name', leftX, currentY);
        currentY += lineHeight + 5;

        // Details
        ctx.fillStyle = design.secondaryColor;
        ctx.font = '16px Arial, sans-serif';

        const details = [
            { label: 'Category:', value: ticketData.category || 'General' },
            { label: 'Date:', value: ticketData.date || 'TBA' },
            { label: 'Venue:', value: ticketData.venue || 'TBA' },
            { label: 'Ticket #:', value: ticketData.ticketNumber || 'TBD' },
            { label: 'Price:', value: ticketData.price ? `$${ticketData.price}` : 'Free' }
        ];

        details.forEach(detail => {
            ctx.fillStyle = design.secondaryColor;
            ctx.fillText(detail.label, leftX, currentY);
            ctx.fillStyle = '#1f2937';
            ctx.font = 'bold 16px Arial, sans-serif';
            ctx.fillText(detail.value, leftX + 80, currentY);
            ctx.font = '16px Arial, sans-serif';
            currentY += lineHeight;
        });

        // Add decorative elements
        this.addDecorativeElements(ctx, design);
    }

    addDecorativeElements(ctx, design) {
        // Add perforated edge effect
        const perfY = design.height - 40;
        ctx.fillStyle = '#e2e8f0';
        for (let x = 20; x < design.width - 20; x += 15) {
            ctx.beginPath();
            ctx.arc(x, perfY, 3, 0, 2 * Math.PI);
            ctx.fill();
        }

        // Add corner decorations
        ctx.fillStyle = design.accentColor;
        ctx.font = '12px Arial, sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('ADMIT ONE', design.width / 2, design.height - 15);
    }

    addDownloadButtons(container, ticketData, canvas, ticketNumber) {
        const buttonContainer = document.createElement('div');
        buttonContainer.className = 'text-center mt-3';

        const downloadBtn = document.createElement('button');
        downloadBtn.className = 'btn btn-outline-primary btn-sm me-2';
        downloadBtn.innerHTML = '<i class="fas fa-download me-1"></i>Download Image';
        downloadBtn.onclick = () => this.downloadTicketImage(canvas, ticketData.ticketNumber || `ticket-${ticketNumber}`);

        const pdfBtn = document.createElement('button');
        pdfBtn.className = 'btn btn-outline-secondary btn-sm';
        pdfBtn.innerHTML = '<i class="fas fa-file-pdf me-1"></i>PDF';
        pdfBtn.onclick = () => this.downloadTicket(ticketData.ticketNumber);

        buttonContainer.appendChild(downloadBtn);
        buttonContainer.appendChild(pdfBtn);
        container.appendChild(buttonContainer);
    }

    drawRoundedRect(ctx, x, y, width, height, radius, fillStyle, topOnly = false) {
        ctx.beginPath();

        if (topOnly) {
            // Only round top corners
            ctx.moveTo(x + radius, y);
            ctx.lineTo(x + width - radius, y);
            ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
            ctx.lineTo(x + width, y + height);
            ctx.lineTo(x, y + height);
            ctx.lineTo(x, y + radius);
            ctx.quadraticCurveTo(x, y, x + radius, y);
        } else {
            ctx.moveTo(x + radius, y);
            ctx.lineTo(x + width - radius, y);
            ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
            ctx.lineTo(x + width, y + height - radius);
            ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
            ctx.lineTo(x + radius, y + height);
            ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
            ctx.lineTo(x, y + radius);
            ctx.quadraticCurveTo(x, y, x + radius, y);
        }

        ctx.closePath();
        ctx.fillStyle = fillStyle;
        ctx.fill();
    }

    downloadTicketImage(canvas, ticketNumber) {
        const link = document.createElement('a');
        link.download = `${ticketNumber}-ticket.png`;
        link.href = canvas.toDataURL('image/png');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    // Fallback method if server request fails
    showFallbackTickets() {
        for (let index = 0; index < this.tickets.length; index++) {
            const elementId = `ticket-image-${index + 1}`;
            const element = document.getElementById(elementId);

            if (element) {
                element.innerHTML = `
                    <div class="alert alert-info text-center">
                        <i class="fas fa-ticket-alt fa-3x mb-3 text-primary"></i>
                        <h5>Ticket ${index + 1}</h5>
                        <p class="mb-2"><strong>Event:</strong> ${this.tickets[index]?.eventName || 'Event Name'}</p>
                        <p class="mb-2"><strong>Category:</strong> ${this.tickets[index]?.category || 'General'}</p>
                        <p class="mb-0"><strong>Ticket #:</strong> ${this.tickets[index]?.ticketNumber || 'TBD'}</p>
                        <hr>
                        <button class="btn btn-outline-primary btn-sm" onclick="downloadTicket('${this.tickets[index]?.ticketNumber}')">
                            <i class="fas fa-download me-1"></i>Download PDF
                        </button>
                    </div>
                `;
            }
        }
    }

    // Enhanced download ticket method
    async downloadTicket(ticketNumber) {
        if (!this.downloadTicketUrl) {
            alert('Download URL not configured');
            return;
        }

        try {
            const response = await fetch(`${this.downloadTicketUrl}?ticketNumber=${ticketNumber}`);

            if (!response.ok) {
                throw new Error('Download failed');
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.style.display = 'none';
            a.href = url;
            a.download = `${ticketNumber}.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);
        } catch (error) {
            console.error('Download error:', error);
            alert('Failed to download ticket. Please try again.');
        }
    }

    // Enhanced email tickets method
    async emailTickets() {
        if (!this.emailTicketsUrl || !this.bookingId) {
            alert('Email functionality not configured');
            return;
        }

        try {
            // Show loading state if button exists
            const button = document.activeElement;
            const originalText = button ? button.innerHTML : '';
            if (button && button.tagName === 'BUTTON') {
                button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';
                button.disabled = true;
            }

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch(this.emailTicketsUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({ bookingId: this.bookingId })
            });

            const result = await response.json();

            if (result.success) {
                // Show success message
                this.showSuccessMessage('Tickets have been sent to your email!');
            } else {
                alert('Failed to send tickets: ' + (result.message || 'Unknown error'));
            }
        } catch (error) {
            console.error('Error sending tickets:', error);
            alert('Failed to send tickets. Please try again.');
        } finally {
            // Restore button state
            const button = document.activeElement;
            if (button && button.tagName === 'BUTTON' && originalText) {
                button.innerHTML = originalText;
                button.disabled = false;
            }
        }
    }

    showSuccessMessage(message) {
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-success alert-dismissible fade show mt-3';
        alertDiv.innerHTML = `
            <i class="fas fa-check-circle me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        // Find a good place to insert the message
        const container = document.querySelector('.booking-confirmation') || document.body;
        container.insertBefore(alertDiv, container.firstChild);

        // Auto-dismiss after 5 seconds
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.remove();
            }
        }, 5000);
    }

    destroy() {
        if (this.countdownTimer) {
            clearInterval(this.countdownTimer);
        }
        this.ticketImages.clear();
    }
}

// Global functions for backward compatibility
function downloadTicket(ticketNumber) {
    if (window.bookingConfirmation) {
        window.bookingConfirmation.downloadTicket(ticketNumber);
    }
}

function emailTickets() {
    if (window.bookingConfirmation) {
        window.bookingConfirmation.emailTickets();
    }
}