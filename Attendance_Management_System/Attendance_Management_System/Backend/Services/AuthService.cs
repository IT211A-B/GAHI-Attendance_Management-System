using System.Text;
using System.Text.Json;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

// Handles authentication operations: login, registration, and user profile retrieval
public class AuthService : IAuthService
{
    private const string GenericInvalidCredentialsMessage = "Invalid email or password.";
    private const string GenericResendVerificationMessage = "If an account exists for that email, a verification link has been sent. Please check your inbox.";
    private const string InvalidEmailConfirmationMessage = "Invalid or expired email confirmation link.";

    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;
    private readonly ISectionAllocationService _sectionAllocationService;
    private readonly IAccountEmailService _accountEmailService;
    private readonly INotificationService _notificationService;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        AppDbContext context,
        ISectionAllocationService sectionAllocationService,
        IAccountEmailService accountEmailService,
        INotificationService notificationService,
        IOptions<EmailSettings> emailSettings,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _context = context;
        _sectionAllocationService = sectionAllocationService;
        _accountEmailService = accountEmailService;
        _notificationService = notificationService;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    // Authenticates user by verifying email and password
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Find user by email address
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Return generic error to prevent email enumeration attacks
        if (user == null)
        {
            return CreateFailureResponse(GenericInvalidCredentialsMessage);
        }

        // Check if account is active
        if (!user.IsActive)
        {
            return CreateFailureResponse("Your account has been deactivated. Please contact administrator.");
        }

        // Verify password against stored hash
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            return CreateFailureResponse(GenericInvalidCredentialsMessage);
        }

        // Build user DTO with role-specific details
        var userDto = await BuildUserDtoAsync(user);

        return CreateSuccessResponse("Login successful.", userDto);
    }

    // Registers a new student and creates pending enrollment request
    public async Task<AuthResponse> RegisterStudentAsync(RegisterRequest request)
    {
        var validationError = await ValidateStudentRegistrationRequestAsync(request);
        if (validationError != null)
        {
            return validationError;
        }

        var sectionResolution = await ResolveStudentRegistrationSectionAsync(request);
        if (sectionResolution.Error != null || sectionResolution.AssignedSection == null)
        {
            return sectionResolution.Error ?? CreateFailureResponse("Unable to complete registration right now. Please try again.");
        }

        var registrationResult = await CreateStudentRegistrationAsync(
            request,
            sectionResolution.AssignedSection,
            sectionResolution.ResolvedYearLevel);

        if (registrationResult.Error != null || registrationResult.User == null)
        {
            return registrationResult.Error ?? CreateFailureResponse("Unable to complete registration right now. Please try again.");
        }

        var studentDisplayName = BuildDisplayName(request.FirstName, request.MiddleName, request.LastName);
        await RunPostStudentRegistrationSideEffectsAsync(registrationResult.User, studentDisplayName, request.StudentNumber);

        var userDto = await BuildUserDtoAsync(registrationResult.User);
        return CreateSuccessResponse("Registration successful. Your enrollment is pending approval.", userDto);
    }

    public async Task<AuthResponse> ConfirmEmailAsync(int userId, string token)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(token))
        {
            return CreateInvalidEmailConfirmationResponse();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return CreateInvalidEmailConfirmationResponse();
        }

        if (user.EmailConfirmed)
        {
            return CreateSuccessResponse("Email is already confirmed. You can sign in.");
        }

        var decodedToken = DecodeEmailToken(token);
        if (string.IsNullOrWhiteSpace(decodedToken))
        {
            return CreateInvalidEmailConfirmationResponse();
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            return CreateInvalidEmailConfirmationResponse();
        }

        return CreateSuccessResponse("Email confirmed successfully. You can now sign in.");
    }

    public async Task<AuthResponse> ResendVerificationAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return CreateSuccessResponse(GenericResendVerificationMessage);
        }

        var normalizedEmail = email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user == null || user.EmailConfirmed || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
        {
            return CreateSuccessResponse(GenericResendVerificationMessage);
        }

        try
        {
            var confirmationLink = await BuildEmailConfirmationLinkAsync(user);
            var displayName = await ResolveDisplayNameAsync(user);

            await _accountEmailService.SendVerificationEmailAsync(user.Email, displayName, confirmationLink);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resend verification email for user {UserId}.", user.Id);
        }

        return CreateSuccessResponse(GenericResendVerificationMessage);
    }

    // Registers a new teacher with auto-generated employee number
    public async Task<AuthResponse> RegisterTeacherAsync(TeacherRegisterRequest request)
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
            Role = "teacher",
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
        var employeeNumber = await GenerateEmployeeNumberAsync();

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
        await _context.SaveChangesAsync();

        var userDto = await BuildUserDtoAsync(user);

        return CreateSuccessResponse("Teacher registration successful.", userDto);
    }

    // Retrieves user profile with role-specific information
    public async Task<UserDto?> GetUserProfileAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        return await BuildUserDtoAsync(user);
    }

    private async Task<AuthResponse?> ValidateStudentRegistrationRequestAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return CreateFailureResponse("An account with this email already exists.");
        }

        // Check if student number already exists
        var existingStudentNumber = await _context.Students
            .AnyAsync(student => student.StudentNumber == request.StudentNumber);
        if (existingStudentNumber)
        {
            return CreateFailureResponse("This student number is already registered.");
        }

        // Validate CourseId exists
        var courseExists = await _context.Courses.AnyAsync(course => course.Id == request.CourseId);
        if (!courseExists)
        {
            return CreateFailureResponse("Invalid course selected.");
        }

        // Validate AcademicYearId exists
        var academicYearExists = await _context.AcademicYears.AnyAsync(academicYear => academicYear.Id == request.AcademicYearId);
        if (!academicYearExists)
        {
            return CreateFailureResponse("Invalid academic year selected.");
        }

        return null;
    }

    private async Task<(Section? AssignedSection, int ResolvedYearLevel, AuthResponse? Error)> ResolveStudentRegistrationSectionAsync(RegisterRequest request)
    {
        var resolvedYearLevel = 1;

        // Optional explicit section still supported for backward compatibility.
        if (request.SectionId.HasValue && request.SectionId.Value > 0)
        {
            var assignedSection = await _context.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(section => section.Id == request.SectionId.Value);

            if (assignedSection == null)
            {
                return (null, resolvedYearLevel, CreateFailureResponse("Invalid section selected."));
            }

            if (assignedSection.CourseId != request.CourseId)
            {
                return (null, resolvedYearLevel, CreateFailureResponse("Selected section does not belong to the selected course."));
            }

            if (assignedSection.AcademicYearId != request.AcademicYearId)
            {
                return (null, resolvedYearLevel, CreateFailureResponse("Selected section does not belong to the selected academic period."));
            }

            resolvedYearLevel = assignedSection.YearLevel > 0 ? assignedSection.YearLevel : 1;
            return (assignedSection, resolvedYearLevel, null);
        }

        var autoAssignedSection = await _sectionAllocationService
            .AllocateSectionAsync(request.CourseId, request.AcademicYearId, resolvedYearLevel);

        if (autoAssignedSection == null)
        {
            return (null, resolvedYearLevel, CreateFailureResponse("No available sections for the selected course and academic period. Please contact an administrator."));
        }

        resolvedYearLevel = autoAssignedSection.YearLevel > 0 ? autoAssignedSection.YearLevel : resolvedYearLevel;
        return (autoAssignedSection, resolvedYearLevel, null);
    }

    private async Task<(User? User, AuthResponse? Error)> CreateStudentRegistrationAsync(
        RegisterRequest request,
        Section assignedSection,
        int resolvedYearLevel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var user = BuildStudentUser(request.Email);
            var userCreateResult = await _userManager.CreateAsync(user, request.Password);
            if (!userCreateResult.Succeeded)
            {
                await transaction.RollbackAsync();
                var errors = string.Join(", ", userCreateResult.Errors.Select(error => error.Description));
                return (null, CreateFailureResponse($"Registration failed: {errors}"));
            }

            var student = BuildStudentEntity(request, user.Id, resolvedYearLevel);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var enrollment = BuildPendingEnrollment(student.Id, assignedSection.Id, request.AcademicYearId);
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return (user, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            return (null, CreateFailureResponse("Unable to complete registration right now. Please try again."));
        }
    }

    private async Task RunPostStudentRegistrationSideEffectsAsync(User user, string studentDisplayName, string studentNumber)
    {
        await TrySendSignupAndVerificationEmailsAsync(user, studentDisplayName);
        await TryNotifyAdminsOfSignupAsync(user, studentDisplayName, studentNumber);
    }

    // Constructs UserDto with student or teacher details based on role
    private async Task<UserDto> BuildUserDtoAsync(User user)
    {
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = user.Role,
            IsActive = user.IsActive
        };

        if (user.Role == "student")
        {
            var student = await _context.Students
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

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
        else if (user.Role == "teacher")
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

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

    private async Task TrySendSignupAndVerificationEmailsAsync(User user, string studentDisplayName)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        try
        {
            var confirmationLink = await BuildEmailConfirmationLinkAsync(user);

            await _accountEmailService.SendSignupAcknowledgmentAsync(user.Email, studentDisplayName);
            await _accountEmailService.SendVerificationEmailAsync(user.Email, studentDisplayName, confirmationLink);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send signup/verification emails for user {UserId}.", user.Id);
        }
    }

    private async Task TryNotifyAdminsOfSignupAsync(User user, string studentDisplayName, string studentNumber)
    {
        try
        {
            var adminUserIds = await _context.Users
                .AsNoTracking()
                .Where(account => account.Role == "admin" && account.IsActive)
                .Select(account => account.Id)
                .ToListAsync();

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

    private async Task<string> ResolveDisplayNameAsync(User user)
    {
        var studentName = await _context.Students
            .AsNoTracking()
            .Where(student => student.UserId == user.Id)
            .Select(student => new { student.FirstName, student.MiddleName, student.LastName })
            .FirstOrDefaultAsync();

        if (studentName != null)
        {
            return BuildDisplayName(studentName.FirstName, studentName.MiddleName, studentName.LastName);
        }

        var teacherName = await _context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.UserId == user.Id)
            .Select(teacher => new { teacher.FirstName, teacher.MiddleName, teacher.LastName })
            .FirstOrDefaultAsync();

        if (teacherName != null)
        {
            return BuildDisplayName(teacherName.FirstName, teacherName.MiddleName, teacherName.LastName);
        }

        return user.Email ?? "Student";
    }

    private async Task<string> BuildEmailConfirmationLinkAsync(User user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return BuildEmailConfirmationLink(user.Id, token);
    }

    private string BuildEmailConfirmationLink(int userId, string token)
    {
        var encodedToken = EncodeEmailConfirmationToken(token);
        var baseUrl = ResolvePublicBaseUrl().TrimEnd('/');
        return $"{baseUrl}/confirm-email?userId={userId}&token={encodedToken}";
    }

    private static string EncodeEmailConfirmationToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private string ResolvePublicBaseUrl()
    {
        return string.IsNullOrWhiteSpace(_emailSettings.PublicBaseUrl)
            ? "https://localhost:5001"
            : _emailSettings.PublicBaseUrl.Trim();
    }

    private static string? DecodeEmailToken(string token)
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

    private static User BuildStudentUser(string email)
    {
        return new User
        {
            UserName = email,
            Email = email,
            Role = "student",
            IsActive = true,
            EmailConfirmed = false
        };
    }

    private static Student BuildStudentEntity(RegisterRequest request, int userId, int resolvedYearLevel)
    {
        return new Student
        {
            UserId = userId,
            CourseId = request.CourseId,
            SectionId = null, // Will be set when enrollment is approved
            StudentNumber = request.StudentNumber,
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
    }

    private static Enrollment BuildPendingEnrollment(int studentId, int sectionId, int academicYearId)
    {
        return new Enrollment
        {
            StudentId = studentId,
            SectionId = sectionId,
            AcademicYearId = academicYearId,
            Status = "pending"
        };
    }

    private static AuthResponse CreateInvalidEmailConfirmationResponse()
    {
        return CreateFailureResponse(InvalidEmailConfirmationMessage);
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

    private async Task<string> GenerateEmployeeNumberAsync()
    {
        // Get the count of teachers to generate sequential employee number
        var teacherCount = await _context.Teachers.CountAsync();
        var year = DateTime.UtcNow.Year;
        return $"EMP{year}{(teacherCount + 1):D4}";
    }
}
