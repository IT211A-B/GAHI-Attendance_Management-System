using Attendance_Management_System.Backend;
using Attendance_Management_System.Backend.Data;
using Attendance_Management_System.Backend.Hubs;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

// Create the web application builder with frontend web root configured up-front
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "Frontend/wwwroot"
});

// Add MVC controllers with views for handling web and API requests
builder.Services.AddControllersWithViews();

// Tell Razor to resolve pages from Frontend/Views instead of root Views
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Frontend/Views/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/Frontend/Views/Shared/{0}.cshtml");
});

// Register all backend services (database, auth, repositories, services)
builder.Services.AddBackend(builder.Configuration);

// Trust reverse proxy headers (Render) for HTTPS scheme and client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add health checks to monitor database connectivity
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

// Build the application from configured services
var app = builder.Build();

app.UseForwardedHeaders();

// Configure production-specific error handling and security
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Force HTTPS redirection for secure connections
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Configure default MVC route pattern
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/hubs/notifications").DisableRateLimiting();

// Expose health check endpoint for monitoring
app.MapHealthChecks("/health").DisableRateLimiting();

// Run database migrations and seed initial data on startup
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Apply any pending database migrations
        await context.Database.MigrateAsync();
        // Populate initial seed data (roles, admin user, etc.)
        await SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during migration or seeding.");
    }
}

// Start the web application
app.Run();

public partial class Program;
