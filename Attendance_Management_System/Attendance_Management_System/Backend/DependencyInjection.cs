using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Repositories;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Repositories;
using Attendance_Management_System.Backend.Services;
using Attendance_Management_System.Backend.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        // Configure ASP.NET Core Identity with password requirements
        services.AddIdentity<User, IdentityRole<int>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
        })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

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

        return services;
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
