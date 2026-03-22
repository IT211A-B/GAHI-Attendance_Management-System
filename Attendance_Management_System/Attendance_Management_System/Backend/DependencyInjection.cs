using System.Text;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Repositories;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Repositories;
using Attendance_Management_System.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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

        // Bind JWT settings from configuration to strongly-typed class
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

        // Validate that JWT secret key is configured
        if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json or environment variables.");
        }

        // Ensure secret key meets minimum length for security
        if (jwtSettings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long.");
        }

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

        // Configure JWT Bearer as the default authentication scheme
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Define token validation parameters for security
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // Register generic repository pattern for data access
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register application services for business logic
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<ITeachersService, TeachersService>();
        services.AddScoped<ISectionsService, SectionsService>();
        services.AddScoped<IClassroomsService, ClassroomsService>();
        services.AddScoped<ICoursesService, CoursesService>();
        services.AddScoped<ISubjectsService, SubjectsService>();
        services.AddScoped<IAcademicYearsService, AcademicYearsService>();
        services.AddScoped<IUsersService, UsersService>();

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
}
