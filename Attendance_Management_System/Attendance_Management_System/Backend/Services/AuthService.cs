using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Attendance_Management_System.Backend.Services;

// Handles authentication operations: login, registration, and user profile retrieval
public class AuthService : IAuthService
{
    private const string StudentNumberPrefix = "STD-";
    private const int StudentNumberGenerationMaxAttempts = 3;

    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly ISectionAllocationService _sectionAllocationService;
    private readonly IAccountEmailService _accountEmailService;
    private readonly INotificationService _notificationService;
    private readonly EmailSettings _emailSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        AppDbContext context,
        ISectionAllocationService sectionAllocationService,
        IAccountEmailService accountEmailService,
        INotificationService notificationService,
        IOptions<EmailSettings> emailSettings,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _context = context;
        _sectionAllocationService = sectionAllocationService;
        _accountEmailService = accountEmailService;
        _notificationService = notificationService;
        _emailSettings = emailSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // Registers a new student and creates pending enrollment request
    public async Task<AuthResponse> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return CreateFailureResponse("An account with this email already exists.");
        }

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(selectedCourse => selectedCourse.Id == request.CourseId, cancellationToken);
        if (course == null)
        {
            return CreateFailureResponse("Invalid course selected.");
        }

        if (!EducationLevelPolicy.IsYearLevelAllowed(course.EducationLevel, request.YearLevel))
        {
            var allowedRange = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel);
            return CreateFailureResponse(
                $"Year level {request.YearLevel} is not valid for {EducationLevelPolicy.ToDisplayLabel(course.EducationLevel)}. Allowed range is {allowedRange.MinYearLevel}-{allowedRange.MaxYearLevel}.");
        }

        var academicYearExists = await _context.AcademicYears
            .AnyAsync(academicYear => academicYear.Id == request.AcademicYearId, cancellationToken);
        if (!academicYearExists)
        {
            return CreateFailureResponse("Invalid academic year selected.");
        }

        var resolvedYearLevel = request.YearLevel;
        Section? assignedSection = null;

        if (request.SectionId.HasValue && request.SectionId.Value > 0)
        {
            assignedSection = await _context.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(section => section.Id == request.SectionId.Value, cancellationToken);

            if (assignedSection == null)
            {
                return CreateFailureResponse("Invalid section selected.");
            }

            if (assignedSection.CourseId != request.CourseId)
            {
                return CreateFailureResponse("Selected section does not belong to the selected course.");
            }

            if (assignedSection.AcademicYearId != request.AcademicYearId)
            {
                return CreateFailureResponse("Selected section does not belong to the selected academic period.");
            }

            if (assignedSection.YearLevel != resolvedYearLevel)
            {
                return CreateFailureResponse("Selected section year level does not match your chosen year level.");
            }
        }
        else
        {
            assignedSection = await _sectionAllocationService
                .AllocateSectionAsync(request.CourseId, request.AcademicYearId, resolvedYearLevel);

            if (assignedSection == null)
            {
                return CreateFailureResponse("No available sections for the selected course and academic period. Please contact an administrator.");
            }

            if (assignedSection.YearLevel != resolvedYearLevel)
            {
                return CreateFailureResponse("No available section matches the selected year level for this program.");
            }
        }

        User? user = null;
        string? studentNumber = null;
        AuthResponse? registrationFailure = null;

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Role = UserRole.Student.ToStorageValue(),
                    IsActive = true,
                    EmailConfirmed = false
                };

                var userCreateResult = await _userManager.CreateAsync(user, request.Password);
                if (!userCreateResult.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    var errors = string.Join(", ", userCreateResult.Errors.Select(error => error.Description));
                    registrationFailure = CreateFailureResponse($"Registration failed: {errors}");
                    return;
                }

                studentNumber = await GenerateStudentNumberAsync(cancellationToken);
                if (studentNumber == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogWarning("Unable to generate a unique student number after {AttemptCount} attempts.", StudentNumberGenerationMaxAttempts);
                    registrationFailure = CreateFailureResponse("Unable to complete registration right now. Please try again.");
                    return;
                }

                var student = new Student
                {
                    UserId = user.Id,
                    CourseId = request.CourseId,
                    SectionId = null,
                    StudentNumber = studentNumber,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    MiddleName = request.MiddleName,
                    Birthdate = request.Birthdate,
                    Gender = request.Gender,
                    Address = request.Address,
                    GuardianName = request.GuardianName,
                    GuardianContact = request.GuardianContact,
                    YearLevel = resolvedYearLevel,
                    IsActive = true
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync(cancellationToken);

                var enrollment = new Enrollment
                {
                    StudentId = student.Id,
                    SectionId = assignedSection.Id,
                    AcademicYearId = request.AcademicYearId,
                    Status = EnrollmentStatus.Pending.ToStorageValue()
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                registrationFailure = CreateFailureResponse("Unable to complete registration right now. Please try again.");
            }
        });

        if (registrationFailure != null)
        {
            return registrationFailure;
        }

        if (user == null || string.IsNullOrWhiteSpace(studentNumber))
        {
            return CreateFailureResponse("Unable to complete registration right now. Please try again.");
        }

        var studentDisplayName = BuildDisplayName(request.FirstName, request.MiddleName, request.LastName);
        await RunPostStudentRegistrationSideEffectsAsync(user, studentDisplayName, studentNumber, cancellationToken);

        var userDto = await BuildUserDtoAsync(user, cancellationToken);
        return CreateSuccessResponse("Registration successful. Your enrollment is pending approval.", userDto);
    }

    public async Task<AuthResponse> ConfirmEmailAsync(int userId, string token, CancellationToken cancellationToken = default)
    {
        const string invalidMessage = "Invalid or expired email confirmation link.";

        if (userId <= 0 || string.IsNullOrWhiteSpace(token))
        {
            return CreateFailureResponse(invalidMessage);
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return CreateFailureResponse(invalidMessage);
        }

        if (user.EmailConfirmed)
        {
            return CreateSuccessResponse("Email is already confirmed. You can sign in.");
        }

        // Confirmation tokens arrive URL-encoded, so decode them before handing them to Identity.
        var decodedToken = DecodeToken(token);
        if (string.IsNullOrWhiteSpace(decodedToken))
        {
            return CreateFailureResponse(invalidMessage);
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return CreateFailureResponse(invalidMessage);
        }

        return CreateSuccessResponse("Email confirmed successfully. You can now sign in.");
    }

    public async Task<AuthResponse> ResendVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        const string responseMessage = "If an account exists for that email, a verification link has been sent. Please check your inbox.";

        if (string.IsNullOrWhiteSpace(email))
        {
            return CreateSuccessResponse(responseMessage);
        }

        var normalizedEmail = email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null || user.EmailConfirmed || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            // Keep the response neutral so callers cannot infer whether an account exists.
            return CreateSuccessResponse(responseMessage);
        }

        try
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = BuildUserLink(user.Id, token, "confirm-email");
            var displayName = await ResolveDisplayNameAsync(user, cancellationToken);

            await _accountEmailService.SendVerificationEmailAsync(user.Email, displayName, confirmationLink);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resend verification email for user {UserId}.", user.Id);
        }

        return CreateSuccessResponse(responseMessage);
    }

    public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        const string responseMessage = "If an account exists for that email, password reset instructions have been sent. Please check your inbox.";

        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return CreateSuccessResponse(responseMessage);
        }

        var normalizedEmail = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogInformation("Ignored forgot-password request for unknown email {Email}.", normalizedEmail);
            // Keep the response neutral so callers cannot infer whether an account exists.
            return CreateSuccessResponse(responseMessage);
        }

        if (!user.IsActive)
        {
            _logger.LogInformation("Ignored forgot-password request for inactive user {UserId}.", user.Id);
            return CreateSuccessResponse(responseMessage);
        }

        try
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = BuildUserLink(user.Id, token, "reset-password");
            var displayName = await ResolveDisplayNameAsync(user, cancellationToken);

            await _accountEmailService.SendPasswordResetEmailAsync(user.Email, displayName, resetLink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email for user {UserId}.", user.Id);
            return CreateFailureResponse("Unable to send password reset email right now. Please try again later.");
        }

        return CreateSuccessResponse(responseMessage);
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        const string invalidLinkMessage = "Invalid or expired password reset link.";

        if (request == null || request.UserId <= 0 || string.IsNullOrWhiteSpace(request.Token))
        {
            return CreateFailureResponse(invalidLinkMessage);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return CreateFailureResponse("New password is required.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return CreateFailureResponse(invalidLinkMessage);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset attempted for inactive user {UserId}.", user.Id);
            return CreateFailureResponse(invalidLinkMessage);
        }

        var decodedToken = DecodeToken(request.Token);
        if (string.IsNullOrWhiteSpace(decodedToken))
        {
            return CreateFailureResponse(invalidLinkMessage);
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => string.Equals(error.Code, "InvalidToken", StringComparison.OrdinalIgnoreCase)))
            {
                // Expired or tampered tokens should read as a generic invalid-link error.
                return CreateFailureResponse(invalidLinkMessage);
            }

            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            return CreateFailureResponse(
                string.IsNullOrWhiteSpace(errors)
                    ? "Unable to reset password right now. Please try again."
                    : $"Unable to reset password: {errors}");
        }

        return CreateSuccessResponse("Password has been reset successfully. You can now sign in.");
    }

    // Registers a new teacher with auto-generated employee number
    public async Task<AuthResponse> RegisterTeacherAsync(TeacherRegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return CreateFailureResponse("An account with this email already exists.");
        }

        // Create user
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = UserRole.Teacher.ToStorageValue(),
            IsActive = true,
            EmailConfirmed = true // Auto-confirm for MVP
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return CreateFailureResponse($"Registration failed: {errors}");
        }

        // Generate employee number (auto-generated)
        var employeeNumber = await GenerateEmployeeNumberAsync(cancellationToken);

        // Create teacher record
        var teacher = new Teacher
        {
            UserId = user.Id,
            EmployeeNumber = employeeNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            Department = request.Department,
            Specialization = request.Specialization
        };

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync(cancellationToken);

        var userDto = await BuildUserDtoAsync(user, cancellationToken);

        return CreateSuccessResponse("Teacher registration successful.", userDto);
    }

    // Retrieves user profile with role-specific information
    public async Task<UserDto?> GetUserProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        return await BuildUserDtoAsync(user, cancellationToken);
    }

    private async Task RunPostStudentRegistrationSideEffectsAsync(User user, string studentDisplayName, string studentNumber, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = BuildUserLink(user.Id, token, "confirm-email");

                await _accountEmailService.SendSignupAcknowledgmentAsync(user.Email, studentDisplayName);
                await _accountEmailService.SendVerificationEmailAsync(user.Email, studentDisplayName, confirmationLink);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send signup/verification emails for user {UserId}.", user.Id);
            }
        }

        try
        {
            var adminRole = UserRole.Admin.ToStorageValue();
            var adminUserIds = await _context.Users
                .AsNoTracking()
                .Where(account => account.Role == adminRole && account.IsActive)
                .Select(account => account.Id)
                .ToListAsync(cancellationToken);

            if (adminUserIds.Count == 0)
            {
                return;
            }

            var payloadJson = JsonSerializer.Serialize(new
            {
                StudentUserId = user.Id,
                StudentNumber = studentNumber,
                StudentName = studentDisplayName
            });

            foreach (var adminUserId in adminUserIds)
            {
                await _notificationService.CreateAsync(
                    adminUserId,
                    NotificationTypes.Signup,
                    "New Student Signup",
                    $"{studentDisplayName} submitted a new signup request.",
                    NotificationLinks.Enrollments,
                    payloadJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admin users about student signup for user {UserId}.", user.Id);
        }
    }

    // Constructs UserDto with student or teacher details based on role
    private async Task<UserDto> BuildUserDtoAsync(User user, CancellationToken cancellationToken = default)
    {
        // The response shape changes with the user's role, so load the matching profile record.
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = user.Role,
            IsActive = user.IsActive
        };

        if (user.Role.IsRole(UserRole.Student))
        {
            var student = await _context.Students
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id, cancellationToken);

            if (student != null)
            {
                userDto.StudentNumber = student.StudentNumber;
                userDto.FirstName = student.FirstName;
                userDto.LastName = student.LastName;
                userDto.MiddleName = student.MiddleName;
                userDto.CourseId = student.CourseId;
                userDto.CourseName = student.Course?.Name;
                userDto.YearLevel = student.YearLevel;
            }
        }
        else if (user.Role.IsRole(UserRole.Teacher))
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id, cancellationToken);

            if (teacher != null)
            {
                userDto.EmployeeNumber = teacher.EmployeeNumber;
                userDto.FirstName = teacher.FirstName;
                userDto.LastName = teacher.LastName;
                userDto.MiddleName = teacher.MiddleName;
                userDto.Department = teacher.Department;
                userDto.Specialization = teacher.Specialization;
            }
        }

        return userDto;
    }

    private async Task<string> ResolveDisplayNameAsync(User user, CancellationToken cancellationToken = default)
    {
        var studentName = await _context.Students
            .AsNoTracking()
            .Where(student => student.UserId == user.Id)
            .Select(student => new { student.FirstName, student.MiddleName, student.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (studentName != null)
        {
            return BuildDisplayName(studentName.FirstName, studentName.MiddleName, studentName.LastName);
        }

        var teacherName = await _context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.UserId == user.Id)
            .Select(teacher => new { teacher.FirstName, teacher.MiddleName, teacher.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherName != null)
        {
            return BuildDisplayName(teacherName.FirstName, teacherName.MiddleName, teacherName.LastName);
        }

        return user.Email ?? "Student";
    }

    private string BuildUserLink(int userId, string token, string path)
    {
        var encodedToken = EncodeToken(token);
        var baseUrl = ResolvePublicBaseUrl().TrimEnd('/');
        return $"{baseUrl}/{path}?userId={userId}&token={encodedToken}";
    }

    private static string EncodeToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private string ResolvePublicBaseUrl()
    {
        var configuredBaseUrl = _emailSettings.PublicBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) && !IsLoopbackBaseUrl(configuredBaseUrl))
        {
            return configuredBaseUrl;
        }

        var requestOrigin = ResolveRequestOrigin();
        if (!string.IsNullOrWhiteSpace(requestOrigin))
        {
            return requestOrigin;
        }

        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl;
        }

        throw new InvalidOperationException($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} is not configured.");
    }

    private string? ResolveRequestOrigin()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null || !request.Host.HasValue)
        {
            return null;
        }

        var scheme = request.Scheme;
        if (!string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return $"{scheme}://{request.Host.Value.TrimEnd('/')}";
    }

    private static bool IsLoopbackBaseUrl(string baseUrl)
    {
        return Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
    }

    private static string? DecodeToken(string token)
    {
        try
        {
            var decodedBytes = WebEncoders.Base64UrlDecode(token.Trim());
            return Encoding.UTF8.GetString(decodedBytes);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildDisplayName(string? firstName, string? middleName, string? lastName)
    {
        var parts = new[] { firstName, middleName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        var fullName = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(fullName) ? "Student" : fullName;
    }

    private async Task<string?> GenerateStudentNumberAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < StudentNumberGenerationMaxAttempts; attempt++)
        {
            var candidate = $"{StudentNumberPrefix}{Guid.NewGuid()}";
            var alreadyExists = await _context.Students
                .AsNoTracking()
                .AnyAsync(student => student.StudentNumber == candidate, cancellationToken);

            if (!alreadyExists)
            {
                return candidate;
            }
        }

        return null;
    }

    private static AuthResponse CreateFailureResponse(string message)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message
        };
    }

    private static AuthResponse CreateSuccessResponse(string message, UserDto? user = null)
    {
        return new AuthResponse
        {
            Success = true,
            Message = message,
            User = user
        };
    }

    private async Task<string> GenerateEmployeeNumberAsync(CancellationToken cancellationToken = default)
    {
        // Get the count of teachers to generate sequential employee number
        var teacherCount = await _context.Teachers.CountAsync(cancellationToken);
        var year = DateTime.UtcNow.Year;
        return $"EMP{year}{(teacherCount + 1):D4}";
    }
}
