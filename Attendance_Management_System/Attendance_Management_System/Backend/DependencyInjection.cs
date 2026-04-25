using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Repositories;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Repositories;
using Attendance_Management_System.Backend.Services;
using Attendance_Management_System.Backend.Security;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend;

// Extension class for registering all backend services and configurations
public static class DependencyInjection
{
    // Registers all backend dependencies (database, auth, services, repositories)
    public static IServiceCollection AddBackend(this IServiceCollection services, IConfiguration configuration)
    {
        // Get database connection string from configuration
        var connectionString = configuration.GetConnectionString("Default");

        // Configure PostgreSQL database context
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Bind cookie settings from configuration to strongly-typed class
        services.Configure<CookieSettings>(configuration.GetSection(CookieSettings.SectionName));

        // Bind enrollment settings from configuration to strongly-typed class
        services.Configure<EnrollmentSettings>(configuration.GetSection(EnrollmentSettings.SectionName));

        // Bind attendance settings from configuration to strongly-typed class
        services.Configure<AttendanceSettings>(configuration.GetSection(AttendanceSettings.SectionName));

        // Bind QR attendance settings from configuration to strongly-typed class
        services.Configure<AttendanceQrSettings>(configuration.GetSection(AttendanceQrSettings.SectionName));

        // Bind email settings from configuration to strongly-typed class
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // Bind rate limiting settings from configuration to strongly-typed class
        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

        var configuredRateLimiting = configuration
            .GetSection(RateLimitingSettings.SectionName)
            .Get<RateLimitingSettings>();
        var rateLimitingSettings = configuredRateLimiting?.IsValid() == true
            ? configuredRateLimiting
            : RateLimitingSettings.Default;

        // Configure ASP.NET Core Identity with password requirements
        services.AddIdentity<User, IdentityRole<int>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.SignIn.RequireConfirmedEmail = true;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSignalR();

        // Configure Identity cookie authentication
        services.ConfigureApplicationCookie(options =>
        {
            var cookieSettings = configuration.GetSection(CookieSettings.SectionName).Get<CookieSettings>()
                                 ?? new CookieSettings();

            options.LoginPath = "/login";
            options.AccessDeniedPath = "/login";

            options.ExpireTimeSpan = TimeSpan.FromHours(cookieSettings.ExpirationHours);
            options.SlidingExpiration = cookieSettings.SlidingExpiration;
            options.Cookie.HttpOnly = cookieSettings.HttpOnly;
            options.Cookie.SameSite = ParseSameSite(cookieSettings.SameSite);
            options.Cookie.SecurePolicy = ParseSecurePolicy(cookieSettings.SecurePolicy);
        });

        // Register generic repository pattern for data access
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register application services for business logic
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceQrService, AttendanceQrService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ISectionAllocationService, SectionAllocationService>();
        services.AddScoped<ITeachersService, TeachersService>();
        services.AddScoped<ISectionsService, SectionsService>();
        services.AddScoped<IClassroomsService, ClassroomsService>();
        services.AddScoped<ICoursesService, CoursesService>();
        services.AddScoped<ISubjectsService, SubjectsService>();
        services.AddScoped<IAcademicYearsService, AcademicYearsService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<ISchedulesService, SchedulesService>();
        services.AddScoped<IConflictService, ConflictService>();
        services.AddScoped<ITeacherHistoryService, TeacherHistoryService>();
        services.AddScoped<IStudentsService, StudentsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotificationPushService, NotificationPushService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAccountEmailService, AccountEmailService>();

        // Add custom claims factory to include User.Role as ClaimTypes.Role
        services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();

        // Define role-based authorization policies for access control
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
            options.AddPolicy("TeacherOnly", policy => policy.RequireRole("teacher"));
            options.AddPolicy("StudentOnly", policy => policy.RequireRole("student"));
            options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole("admin", "teacher"));
            options.AddPolicy("AllRoles", policy => policy.RequireRole("admin", "teacher", "student"));
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfterSeconds = 0;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                }

                if (ShouldReturnJsonRateLimitResponse(context.HttpContext.Request))
                {
                    var details = retryAfterSeconds > 0
                        ? new { retryAfterSeconds }
                        : null;

                    var payload = ApiResponse<object>.ErrorResponse(
                        ErrorCodes.TooManyRequests,
                        "Too many requests. Please try again later.",
                        details);

                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(payload, cancellationToken);
                    return;
                }

                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: BuildRateLimitPartitionKey(httpContext),
                    factory: _ => CreateFixedWindowOptions(rateLimitingSettings.Global)));

            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthLogin, rateLimitingSettings.AuthLogin);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthSignup, rateLimitingSettings.AuthSignup);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthResendVerification, rateLimitingSettings.AuthResendVerification);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.QrSessionMutation, rateLimitingSettings.QrSessionMutations);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.QrCheckin, rateLimitingSettings.QrCheckins);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.QrLiveFeed, rateLimitingSettings.QrLiveFeed);
        });

        return services;
    }

    private static void AddPartitionedFixedWindowPolicy(RateLimiterOptions options, string policyName, FixedWindowPolicySettings settings)
    {
        options.AddPolicy<string>(policyName, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: BuildRateLimitPartitionKey(httpContext),
                factory: _ => CreateFixedWindowOptions(settings)));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(FixedWindowPolicySettings settings)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = settings.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };
    }

    private static string BuildRateLimitPartitionKey(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }

            var userName = httpContext.User.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(userName))
            {
                return $"user:{userName}";
            }
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? "ip:unknown" : $"ip:{remoteIp}";
    }

    private static bool ShouldReturnJsonRateLimitResponse(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/attendance/qr/options")
            || request.Path.StartsWithSegments("/attendance/qr/sessions")
            || request.Path.Equals("/attendance/qr/checkins", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (request.Headers.Accept.Any(value => value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(request.ContentType)
            && request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    // Converts string configuration value to SameSiteMode enum
    private static SameSiteMode ParseSameSite(string value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "none" => SameSiteMode.None,
            "strict" => SameSiteMode.Strict,
            "lax" => SameSiteMode.Lax,
            _ => SameSiteMode.Lax
        };
    }

    // Converts string configuration value to CookieSecurePolicy enum
    private static CookieSecurePolicy ParseSecurePolicy(string value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "always" => CookieSecurePolicy.Always,
            "none" => CookieSecurePolicy.None,
            "sameasrequest" => CookieSecurePolicy.SameAsRequest,
            _ => CookieSecurePolicy.SameAsRequest
        };
    }
}
