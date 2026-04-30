using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;

namespace Attendance_Management_System.Backend.Controllers;

[Route("")]
public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly IAuthService _authService;
    private readonly ICoursesService _coursesService;
    private readonly IAcademicYearsService _academicYearsService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<User> signInManager,
        IAuthService authService,
        ICoursesService coursesService,
        IAcademicYearsService academicYearsService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _authService = authService;
        _coursesService = coursesService;
        _academicYearsService = academicYearsService;
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
            _logger.LogError(ex, "PostgreSQL authentication failed while processing login for {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, "Login is temporarily unavailable due to a database configuration issue.");
            return View(model);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "PostgreSQL connection failure while processing login for {Email}.", model.Email);
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
    public async Task<IActionResult> ConfirmEmail([FromQuery] int userId, [FromQuery] string token)
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
            var coursesResult = await _coursesService.GetAllCoursesAsync();
            if (!coursesResult.Success || coursesResult.Data is null)
            {
                model.ErrorMessage ??= coursesResult.Error?.Message ?? "Unable to load course options right now.";
            }
            else
            {
                model.AvailableCourses = coursesResult.Data
                    .OrderBy(course => course.Name)
                    .Select(course => new SignupCourseOptionViewModel
                    {
                        Id = course.Id,
                        Label = string.IsNullOrWhiteSpace(course.Code)
                            ? course.Name
                            : $"{course.Code} - {course.Name}"
                    })
                    .ToList();
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

        try
        {
            var academicYearsResult = await _academicYearsService.GetAllAcademicYearsAsync();
            if (!academicYearsResult.Success || academicYearsResult.Data is null)
            {
                model.ErrorMessage ??= academicYearsResult.Error?.Message ?? "Unable to load academic period options right now.";
            }
            else
            {
                model.AvailableAcademicYears = academicYearsResult.Data
                    .OrderByDescending(year => year.StartDate)
                    .Select(year => new SignupAcademicYearOptionViewModel
                    {
                        Id = year.Id,
                        Label = year.YearLabel
                    })
                    .ToList();
            }
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

    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}