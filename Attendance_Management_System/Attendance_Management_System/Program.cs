using Attendance_Management_System.Backend;
using Attendance_Management_System.Backend.Data;
using Attendance_Management_System.Backend.Hubs;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// Create the web application builder with frontend web root configured up-front
var webRootPath = Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Frontend", "wwwroot"))
    ? "Frontend/wwwroot"
    : "wwwroot";

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRootPath
});

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}
else
{
    builder.Logging.AddJsonConsole();
}

// Add MVC controllers with views for handling web and API requests
builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient("AttendanceAPI", client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var parsedBaseUrl))
    {
        client.BaseAddress = parsedBaseUrl;
    }
});

// Tell Razor to resolve pages from Frontend/Views instead of root Views
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Frontend/Views/{1}/{0}.cshtml");
    options.ViewLocationFormats.Add("/Frontend/Views/Shared/{0}.cshtml");
});

// Register all backend services (database, auth, repositories, services)
builder.Services.AddBackend(builder.Configuration);

// Register API explorer and Swagger generation for MVC controller endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Trust reverse proxy headers (Render) for HTTPS scheme and client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "application/json",
            "text/css",
            "text/javascript"
        });
    });
}

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? Array.Empty<string>();
var enableCors = corsOrigins.Length > 0;
if (enableCors)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCors", policy =>
            policy.WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

// Add health checks to monitor database connectivity
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

// Build the application from configured services
var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Application started."));
app.Lifetime.ApplicationStopping.Register(() =>
    app.Logger.LogInformation("Application stopping."));
app.Lifetime.ApplicationStopped.Register(() =>
    app.Logger.LogInformation("Application stopped."));

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Force HTTPS redirection for secure connections
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; img-src 'self' data:; font-src 'self' data:; " +
            "style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; " +
            "connect-src 'self' https:; object-src 'none'; base-uri 'self'; " +
            "frame-ancestors 'none'";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        await next();
    });
}

app.UseStaticFiles();
app.UseRouting();

if (!app.Environment.IsDevelopment() && enableCors)
{
    app.UseCors("DefaultCors");
}

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

// Configure default MVC route pattern
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json");
    });
}

app.MapHub<NotificationHub>("/hubs/notifications").DisableRateLimiting();

// Expose health check endpoint for monitoring
app.MapHealthChecks("/health").DisableRateLimiting();

// Run database migrations and seed initial data on startup
// Tests own their schema and seed data, so skip startup writes in that environment.
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
        logger.LogCritical(ex, "Application startup aborted because database migration or seeding failed.");
        throw;
    }
}

// Start the web application
app.Run();

public partial class Program;