using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using StarTickets.Data;
using StarTickets.Models.Configuration;
using StarTickets.Repositories;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services;
using StarTickets.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF license setup
QuestPDF.Settings.License = LicenseType.Community;

// Add EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Add repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IHomeRepository, HomeRepository>();
builder.Services.AddScoped<IEventManagementRepository, EventManagementRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentManagementRepository, PaymentManagementRepository>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<ICategoryManagementRepository, CategoryManagementRepository>();
builder.Services.AddScoped<IUserManagementRepository, UserManagementRepository>();
builder.Services.AddScoped<IVenueManagementRepository, VenueManagementRepository>();
builder.Services.AddScoped<IBookingManagementRepository, BookingManagementRepository>();
builder.Services.AddScoped<IDashboardStatsRepository, DashboardStatsRepository>();
builder.Services.AddScoped<IEventOrganizerRepository, EventOrganizerRepository>();
builder.Services.AddScoped<IReviewsManagementRepository, ReviewsManagementRepository>();

// Add services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IEventManagementService, EventManagementService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentManagementService, PaymentManagementService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<ICategoryManagementService, CategoryManagementService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IVenueManagementService, VenueManagementService>();
builder.Services.AddScoped<IBookingManagementService, BookingManagementService>();
builder.Services.AddScoped<IDashboardStatsService, DashboardStatsService>();
builder.Services.AddScoped<IEventOrganizerService, EventOrganizerService>();
builder.Services.AddScoped<IReviewsManagementService, ReviewsManagementService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

// Add IUrlHelper for AuthService
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddScoped<IUrlHelper>(provider =>
{
    var actionContext = provider.GetRequiredService<IActionContextAccessor>().ActionContext;
    return new UrlHelper(actionContext);
});

// Add session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add controllers with views
builder.Services.AddControllersWithViews();

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();