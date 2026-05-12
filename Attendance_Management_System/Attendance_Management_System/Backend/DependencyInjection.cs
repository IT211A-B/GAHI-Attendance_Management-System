using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Services;
using Attendance_Management_System.Backend.Security;
using FluentEmail.MailKitSmtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Security.Claims;

namespace Attendance_Management_System.Backend;

public static class DependencyInjection
{
    public static IServiceCollection AddBackend(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure()));

        services.Configure<AttendanceSettings>(configuration.GetSection(AttendanceSettings.SectionName));
        services.Configure<AttendanceQrSettings>(configuration.GetSection(AttendanceQrSettings.SectionName));

        var emailSettingsSection = configuration.GetSection(EmailSettings.SectionName);
        services.Configure<EmailSettings>(emailSettingsSection);

        var emailSettings = emailSettingsSection.Get<EmailSettings>() ?? new EmailSettings();

        services
            .AddFluentEmail(emailSettings.FromAddress, emailSettings.FromName)
            .AddMailKitSender(new SmtpClientOptions
            {
                Server = emailSettings.Host,
                Port = emailSettings.Port,
                User = emailSettings.Username,
                Password = emailSettings.Password,
                RequiresAuthentication = !string.IsNullOrWhiteSpace(emailSettings.Username)
                    && !string.IsNullOrWhiteSpace(emailSettings.Password),
                UsePickupDirectory = false,
                UseSsl = emailSettings.UseSsl
            });

        services.Configure<EnrollmentSettings>(configuration.GetSection(EnrollmentSettings.SectionName));
        services.Configure<RateLimitingSettings>(configuration.GetSection(RateLimitingSettings.SectionName));

        var rateLimitingSettings = configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
            ?? RateLimitingSettings.Default;

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

            options.Events.OnRedirectToLogin = context =>
            {
                if (IsQrApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (IsQrApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISectionPageService, SectionPageService>();
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

        services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToStorageValue()));
            options.AddPolicy("TeacherOnly", policy => policy.RequireRole(UserRole.Teacher.ToStorageValue()));
            options.AddPolicy("StudentOnly", policy => policy.RequireRole(UserRole.Student.ToStorageValue()));
            options.AddPolicy("AdminOrTeacher", policy => policy.RequireRole(UserRole.Admin.ToStorageValue(), UserRole.Teacher.ToStorageValue()));
            options.AddPolicy("AllRoles", policy => policy.RequireRole(UserRole.Admin.ToStorageValue(), UserRole.Teacher.ToStorageValue(), UserRole.Student.ToStorageValue()));
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    context.HttpContext.Response.Headers[HeaderNames.RetryAfter] =
                        retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                }

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Please retry after the indicated delay."
                };

                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => CreateFixedWindowOptions(rateLimitingSettings.Global)));

            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthLogin, rateLimitingSettings.AuthLogin);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthSignup, rateLimitingSettings.AuthSignup);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthResendVerification, rateLimitingSettings.AuthResendVerification);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthForgotPassword, rateLimitingSettings.AuthForgotPassword);
            AddPartitionedFixedWindowPolicy(options, RateLimitingPolicyNames.AuthResetPassword, rateLimitingSettings.AuthResetPassword);
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
                partitionKey: GetClientKey(httpContext),
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

    private static string GetClientKey(HttpContext httpContext)
    {
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? "ip:unknown" : $"ip:{remoteIp}";
    }

    private static Microsoft.AspNetCore.Http.SameSiteMode ParseSameSite(string? sameSite)
    {
        return Enum.TryParse<Microsoft.AspNetCore.Http.SameSiteMode>(sameSite, ignoreCase: true, out var parsed)
            ? parsed
            : Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    }

    private static CookieSecurePolicy ParseSecurePolicy(string? securePolicy)
    {
        return Enum.TryParse<CookieSecurePolicy>(securePolicy, ignoreCase: true, out var parsed)
            ? parsed
            : CookieSecurePolicy.SameAsRequest;
    }

    private static bool IsQrApiRequest(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/attendance/qr", out var remaining)
            && remaining.HasValue;
    }
}