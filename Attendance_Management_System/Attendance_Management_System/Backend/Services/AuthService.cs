using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Handles authentication operations: login, registration, and user profile retrieval
public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly AppDbContext _context;

    public AuthService(
        UserManager<User> userManager,
        ITokenService tokenService,
        AppDbContext context)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password."
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Your account has been deactivated. Please contact administrator."
            };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!isPasswordValid)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password."
            };
        }

        var token = _tokenService.GenerateToken(user);
        var userDto = await BuildUserDtoAsync(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            User = userDto
        };
    }

    public async Task<AuthResponse> RegisterStudentAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "An account with this email already exists."
            };
        }

        // Check if student number already exists
        var existingStudentNumber = await _context.Students
            .AnyAsync(s => s.StudentNumber == request.StudentNumber);
        if (existingStudentNumber)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "This student number is already registered."
            };
        }

        // Validate CourseId exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid course selected."
            };
        }

        // Validate SectionId exists
        var sectionExists = await _context.Sections.AnyAsync(s => s.Id == request.SectionId);
        if (!sectionExists)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid section selected."
            };
        }

        // Validate AcademicYearId exists
        var academicYearExists = await _context.AcademicYears.AnyAsync(ay => ay.Id == request.AcademicYearId);
        if (!academicYearExists)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid academic year selected."
            };
        }

        // Create user
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = "student",
            IsActive = true,
            EmailConfirmed = true // Auto-confirm for MVP
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new AuthResponse
            {
                Success = false,
                Message = $"Registration failed: {errors}"
            };
        }

        // Create student record
        var student = new Student
        {
            UserId = user.Id,
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
            YearLevel = 1, // Default to first year
            IsActive = true
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Create pending enrollment
        var enrollment = new Enrollment
        {
            StudentId = student.Id,
            SectionId = request.SectionId,
            AcademicYearId = request.AcademicYearId,
            Status = "pending"
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        // Generate token and response
        var token = _tokenService.GenerateToken(user);
        var userDto = await BuildUserDtoAsync(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful. Your enrollment is pending approval.",
            Token = token,
            User = userDto
        };
    }

    public async Task<AuthResponse> RegisterTeacherAsync(TeacherRegisterRequest request)
    {
        // Check if email already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "An account with this email already exists."
            };
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
            return new AuthResponse
            {
                Success = false,
                Message = $"Registration failed: {errors}"
            };
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

        // Generate token and response
        var token = _tokenService.GenerateToken(user);
        var userDto = await BuildUserDtoAsync(user);

        return new AuthResponse
        {
            Success = true,
            Message = "Teacher registration successful.",
            Token = token,
            User = userDto
        };
    }

    public async Task<UserDto?> GetUserProfileAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        return await BuildUserDtoAsync(user);
    }

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

    private async Task<string> GenerateEmployeeNumberAsync()
    {
        // Get the count of teachers to generate sequential employee number
        var teacherCount = await _context.Teachers.CountAsync();
        var year = DateTime.UtcNow.Year;
        return $"EMP{year}{(teacherCount + 1):D4}";
    }
}