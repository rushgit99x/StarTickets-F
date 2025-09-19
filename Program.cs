using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models.Configuration;
using StarTickets.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// QuestPDF license setup
QuestPDF.Settings.License = LicenseType.Community;


// Add EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

//customer dashboard
builder.Services.AddScoped<ICustomerService, CustomerService>();

// Add session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register PDF Report Service
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // if you’re using Identity
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
