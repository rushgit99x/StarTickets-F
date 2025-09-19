// Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarTickets.Models;
using System;
using System.Net.Sockets;

namespace StarTickets.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // User Management
        public DbSet<User> Users { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        // Event Management
        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<EventRating> EventRatings { get; set; }
        public DbSet<Venue> Venues { get; set; }

        // Booking Management
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingDetail> BookingDetails { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketCategory> TicketCategories { get; set; }

        // Communication
        public DbSet<EmailNotification> EmailNotifications { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }

        // System
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<PaymentGateway> PaymentGateways { get; set; }
        public DbSet<PromotionalCampaign> PromotionalCampaigns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).HasMaxLength(191);
                entity.HasOne(d => d.UserRole)
                      .WithMany(p => p.Users)
                      .HasForeignKey(d => d.Role)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // UserRole Configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.HasIndex(e => e.RoleName).IsUnique();
            });

            // Configure decimal properties for bookings and payments
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.FinalAmount).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<BookingDetail>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<TicketCategory>(entity =>
            {
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
            });

            modelBuilder.Entity<PromotionalCampaign>(entity =>
            {
                entity.Property(e => e.DiscountValue).HasColumnType("decimal(10,2)");
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed UserRoles
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { RoleId = 1, RoleName = "Admin", Description = "System Administrator with full access", CreatedAt = DateTime.UtcNow },
                new UserRole { RoleId = 2, RoleName = "EventOrganizer", Description = "Event Organizer who can create and manage events", CreatedAt = DateTime.UtcNow },
                new UserRole { RoleId = 3, RoleName = "Customer", Description = "Regular customer who can browse and book tickets", CreatedAt = DateTime.UtcNow }
            );

            // Seed Event Categories
            modelBuilder.Entity<EventCategory>().HasData(
                new EventCategory { CategoryId = 1, CategoryName = "Concert", Description = "Live music performances and concerts", CreatedAt = DateTime.UtcNow },
                new EventCategory { CategoryId = 2, CategoryName = "Theatre", Description = "Theatre shows and performances", CreatedAt = DateTime.UtcNow },
                new EventCategory { CategoryId = 3, CategoryName = "Cultural", Description = "Cultural events and festivals", CreatedAt = DateTime.UtcNow },
                new EventCategory { CategoryId = 4, CategoryName = "Sports", Description = "Sporting events and competitions", CreatedAt = DateTime.UtcNow },
                new EventCategory { CategoryId = 5, CategoryName = "Conference", Description = "Business conferences and seminars", CreatedAt = DateTime.UtcNow },
                new EventCategory { CategoryId = 6, CategoryName = "Workshop", Description = "Educational workshops and training", CreatedAt = DateTime.UtcNow }
            );

            // Seed System Settings
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting { SettingId = 1, SettingKey = "SiteName", SettingValue = "StarTickets", Description = "Website name", UpdatedAt = DateTime.UtcNow },
                new SystemSetting { SettingId = 2, SettingKey = "SiteEmail", SettingValue = "admin@startickets.com", Description = "Default system email", UpdatedAt = DateTime.UtcNow },
                new SystemSetting { SettingId = 3, SettingKey = "TicketReservationTimeMinutes", SettingValue = "15", Description = "Minutes to hold ticket reservation during checkout", UpdatedAt = DateTime.UtcNow },
                new SystemSetting { SettingId = 4, SettingKey = "LoyaltyPointsPerDollar", SettingValue = "10", Description = "Loyalty points earned per dollar spent", UpdatedAt = DateTime.UtcNow },
                new SystemSetting { SettingId = 5, SettingKey = "MaxTicketsPerBooking", SettingValue = "10", Description = "Maximum tickets allowed per booking", UpdatedAt = DateTime.UtcNow }
            );

            // Seed Email Templates
            modelBuilder.Entity<EmailTemplate>().HasData(
                new EmailTemplate
                {
                    TemplateId = 1,
                    TemplateName = "BookingConfirmation",
                    Subject = "Booking Confirmation - {EventName}",
                    Body = "Dear {CustomerName},<br><br>Your booking for {EventName} has been confirmed.<br>Booking Reference: {BookingReference}<br><br>Thank you for choosing StarTickets!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new EmailTemplate
                {
                    TemplateId = 2,
                    TemplateName = "TicketDelivery",
                    Subject = "Your E-Tickets for {EventName}",
                    Body = "Dear {CustomerName},<br><br>Please find your e-tickets attached for {EventName}.<br><br>Event Details:<br>Date: {EventDate}<br>Venue: {VenueName}<br><br>See you at the event!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new EmailTemplate
                {
                    TemplateId = 3,
                    TemplateName = "EventReminder",
                    Subject = "Event Reminder - {EventName}",
                    Body = "Dear {CustomerName},<br><br>This is a reminder that {EventName} is coming up on {EventDate}.<br><br>Don't forget to bring your tickets!",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}