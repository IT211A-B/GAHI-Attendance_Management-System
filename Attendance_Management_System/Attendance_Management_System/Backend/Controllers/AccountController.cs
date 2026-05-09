using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Attendance_Management_System.Backend.Controllers;

[Route("")]
public class AccountController : Controller
{
    private const string GenericForgotPasswordNotice = "If an account exists for that email, password reset instructions have been sent. Please check your inbox.";

    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IAuthService _authService;
    private readonly ICoursesService _coursesService;
    private readonly IAcademicYearsService _academicYearsService;
    private readonly AppDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IAuthService authService,
        ICoursesService coursesService,
        IAcademicYearsService academicYearsService,
        AppDbContext context,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _authService = authService;
        _coursesService = coursesService;
        _academicYearsService = academicYearsService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (TempData["AuthError"] is string authError)
        {
            ModelState.AddModelError(string.Empty, authError);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpGet("forgot-password")]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new ForgotPasswordViewModel());
    }


    [HttpGet("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var viewModel = new StudentSignupViewModel();
        await PopulateSignupOptionsAsync(viewModel);
        return View(viewModel);
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthLogin)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await EnsureReviewedStudentAccountCanSignInAsync(model.Email);

        Microsoft.AspNetCore.Identity.SignInResult result;
        try
        {
            result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            _logger.LogError(ex, "PostgreSQL authentication failed while processing login.");
            ModelState.AddModelError(string.Empty, "Login is temporarily unavailable due to a database configuration issue.");
            return View(model);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "PostgreSQL connection failure while processing login.");
            ModelState.AddModelError(string.Empty, "Login is temporarily unavailable. Please try again later.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            if (result.IsNotAllowed)
            {
                ViewData["EmailVerificationRequired"] = true;
                ViewData["EmailVerificationAddress"] = model.Email;
                ModelState.AddModelError(string.Empty, "Please verify your email before signing in. You can request a new verification link below.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthForgotPassword)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new ForgotPasswordRequest
        {
            Email = model.Email.Trim()
        };

        var result = await _authService.ForgotPasswordAsync(request);
        if (!result.Success)
        {
            _logger.LogWarning("ForgotPasswordAsync returned non-success for forgot-password request.");
        }

        TempData["AuthSuccess"] = GenericForgotPasswordNotice;
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpPost("signup")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthSignup)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(StudentSignupViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        await PopulateSignupOptionsAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new RegisterRequest
        {
            Email = model.Email.Trim(),
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            StudentNumber = model.StudentNumber.Trim(),
            FirstName = model.FirstName.Trim(),
            MiddleName = NormalizeOptional(model.MiddleName),
            LastName = model.LastName.Trim(),
            Birthdate = model.Birthdate,
            Gender = model.Gender.Trim(),
            Address = model.Address.Trim(),
            GuardianName = model.GuardianName.Trim(),
            GuardianContact = model.GuardianContact.Trim(),
            CourseId = model.CourseId,
            YearLevel = model.YearLevel,
            AcademicYearId = model.AcademicYearId
        };

        var result = await _authService.RegisterStudentAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Unable to complete registration right now.");
            return View(model);
        }

        TempData["AuthSuccess"] = result.Message ?? "Registration successful. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(int userId, string token)
    {
        var result = await _authService.ConfirmEmailAsync(userId, token);
        if (result.Success)
        {
            TempData["AuthSuccess"] = result.Message;
        }
        else
        {
            TempData["AuthError"] = result.Message;
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet("reset-password")]
    [AllowAnonymous]
    public IActionResult ResetPassword(int userId, [FromQuery] string? token)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new ResetPasswordViewModel
        {
            UserId = userId,
            Token = token ?? string.Empty
        };

        if (userId <= 0 || string.IsNullOrWhiteSpace(token))
        {
            ModelState.AddModelError(string.Empty, "Invalid or expired password reset link.");
        }

        return View(model);
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthResetPassword)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new ResetPasswordRequest
        {
            UserId = model.UserId,
            Token = model.Token,
            NewPassword = model.NewPassword
        };

        var result = await _authService.ResetPasswordAsync(request);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "Unable to reset password right now.");
            return View(model);
        }

        TempData["AuthSuccess"] = result.Message ?? "Password has been reset successfully. You can now sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting(RateLimitingPolicyNames.AuthResendVerification)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerification([FromForm] string email)
    {
        var result = await _authService.ResendVerificationAsync(email);
        TempData["ResendSuccess"] = result.Message;
        TempData["AuthSuccess"] = result.Message;

        return RedirectToAction(nameof(Login));
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private async Task PopulateSignupOptionsAsync(StudentSignupViewModel model)
    {
        try
        {
            var courses = await _coursesService.GetAllCoursesAsync();
            model.AvailableCourses = courses
                .OrderBy(course => course.Name)
                .Select(course => new SignupCourseOptionViewModel
                {
                    Id = course.Id,
                    Label = string.IsNullOrWhiteSpace(course.Code)
                        ? course.Name
                        : $"{course.Code} - {course.Name}",
                    EducationLevel = course.EducationLevel,
                    EducationLevelLabel = EducationLevelPolicy.ToDisplayLabel(course.EducationLevel),
                    MinYearLevel = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel).MinYearLevel,
                    MaxYearLevel = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel).MaxYearLevel
                })
                .ToList();

            if (model.YearLevel <= 0)
            {
                var selectedCourse = model.AvailableCourses.FirstOrDefault(course => course.Id == model.CourseId)
                    ?? model.AvailableCourses.FirstOrDefault();

                if (selectedCourse != null)
                {
                    model.YearLevel = selectedCourse.MinYearLevel;
                }
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            _logger.LogError(ex, "PostgreSQL authentication failed while loading signup course options.");
            model.ErrorMessage ??= "Signup is temporarily unavailable due to a database configuration issue.";
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "PostgreSQL connection failure while loading signup course options.");
            model.ErrorMessage ??= "Unable to load signup options right now. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading signup course options.");
            model.ErrorMessage ??= "Unable to load course options right now.";
        }

        try
        {
            var academicYears = await _academicYearsService.GetAllAcademicYearsAsync();
            model.AvailableAcademicYears = academicYears
                .OrderByDescending(year => year.StartDate)
                .Select(year => new SignupAcademicYearOptionViewModel
                {
                    Id = year.Id,
                    Label = year.YearLabel
                })
                .ToList();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            _logger.LogError(ex, "PostgreSQL authentication failed while loading signup academic period options.");
            model.ErrorMessage ??= "Signup is temporarily unavailable due to a database configuration issue.";
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "PostgreSQL connection failure while loading signup academic period options.");
            model.ErrorMessage ??= "Unable to load signup options right now. Please try again later.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while loading signup academic period options.");
            model.ErrorMessage ??= "Unable to load academic period options right now.";
        }

    }


    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async Task EnsureReviewedStudentAccountCanSignInAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null || user.Role != "student" || user.EmailConfirmed)
        {
            return;
        }

        var hasReviewedEnrollment = await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(enrollment =>
                (enrollment.Status == "approved" || enrollment.Status == "rejected")
                && enrollment.Student != null
                && enrollment.Student.UserId == user.Id);

        if (!hasReviewedEnrollment)
        {
            return;
        }

        user.IsActive = true;
        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);
    }
}
