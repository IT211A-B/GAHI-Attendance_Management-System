using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
[Route("enrollments")]
public class EnrollmentsController : Controller
{
    private const int DefaultPageSize = 20;

    private readonly IEnrollmentService _enrollmentService;
    private readonly IAcademicYearsService _academicYearsService;
    private readonly ISectionsService _sectionsService;
    private readonly ICoursesService _coursesService;
    private readonly IStudentsService _studentsService;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        IAcademicYearsService academicYearsService,
        ISectionsService sectionsService,
        ICoursesService coursesService,
        IStudentsService studentsService,
        ILogger<EnrollmentsController> logger)
    {
        _enrollmentService = enrollmentService;
        _academicYearsService = academicYearsService;
        _sectionsService = sectionsService;
        _coursesService = coursesService;
        _studentsService = studentsService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? status,
        [FromQuery] int? academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (!int.TryParse(userIdClaim, out var userId) || (!role.IsRole(UserRole.Admin) && !role.IsRole(UserRole.Student)))
        {
            return Forbid();
        }

        var safePage = Math.Max(1, page);
        var safePageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, 100);

        var viewModel = await BuildIndexViewModelAsync(
            role,
            NormalizeStatus(status),
            academicYearId,
            safePage,
            safePageSize);

        return View(viewModel);
    }

    [HttpPost("create")]
    [Authorize(Policy = "StudentOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateEnrollmentFormViewModel form)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Challenge();
        }

        var viewModel = await BuildIndexViewModelAsync(
            role: UserRole.Student.ToStorageValue(),
            status: null,
            academicYearId: form.AcademicYearId > 0 ? form.AcademicYearId : null,
            page: 1,
            pageSize: DefaultPageSize);

        viewModel.CreateForm = form;

        if (viewModel.StudentProfile?.CourseId is int lockedCourseId)
        {
            form.CourseId = lockedCourseId;
            viewModel.CreateForm.CourseId = lockedCourseId;
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        try
        {
            var enrollmentResult = await _enrollmentService.CreateEnrollmentAsync(new CreateEnrollmentRequest
            {
                CourseId = form.CourseId,
                YearLevel = form.YearLevel,
                AcademicYearId = form.AcademicYearId
            }, userId);

            TempData["EnrollmentsSuccess"] = string.IsNullOrWhiteSpace(enrollmentResult.Message)
                ? "Enrollment request submitted successfully."
                : enrollmentResult.Message;
            return RedirectToAction(nameof(Index), new { academicYearId = form.AcademicYearId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit enrollment request for user {UserId}.", userId);
            ModelState.AddModelError(string.Empty, "Unable to submit enrollment request right now.");
            return View(nameof(Index), viewModel);
        }
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        int id,
        string? status,
        int? academicYearId,
        int page = 1)
    {
        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(adminIdClaim, out var adminId))
        {
            return Challenge();
        }

        try
        {
            await _enrollmentService.UpdateEnrollmentStatusAsync(id, new UpdateEnrollmentStatusRequest
            {
                Status = EnrollmentStatus.Approved.ToStorageValue()
            }, adminId);

            TempData["EnrollmentsSuccess"] = "Enrollment approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve enrollment {EnrollmentId}.", id);
            TempData["EnrollmentsError"] = "Unable to approve enrollment right now.";
        }

        return RedirectToAction(nameof(Index), new { status = NormalizeStatus(status), academicYearId, page = Math.Max(1, page) });
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(
        int id,
        [Bind(Prefix = "RejectForm")] RejectEnrollmentFormViewModel form,
        string? status,
        int? academicYearId,
        int page = 1)
    {
        if (!ModelState.IsValid)
        {
            TempData["EnrollmentsError"] = "A rejection reason is required.";
            return RedirectToAction(nameof(Index), new { status = NormalizeStatus(status), academicYearId, page = Math.Max(1, page) });
        }

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(adminIdClaim, out var adminId))
        {
            return Challenge();
        }

        try
        {
            await _enrollmentService.UpdateEnrollmentStatusAsync(id, new UpdateEnrollmentStatusRequest
            {
                Status = EnrollmentStatus.Rejected.ToStorageValue(),
                RejectionReason = form.RejectionReason.Trim()
            }, adminId);

            TempData["EnrollmentsSuccess"] = "Enrollment rejected successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject enrollment {EnrollmentId}.", id);
            TempData["EnrollmentsError"] = "Unable to reject enrollment right now.";
        }

        return RedirectToAction(nameof(Index), new { status = NormalizeStatus(status), academicYearId, page = Math.Max(1, page) });
    }

    [HttpPost("{id:int}/reassign")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reassign(
        int id,
        [Bind(Prefix = "ReassignForm")] ReassignEnrollmentFormViewModel form,
        string? status,
        int? academicYearId,
        int page = 1)
    {
        if (!ModelState.IsValid)
        {
            TempData["EnrollmentsError"] = "Please select a valid target section.";
            return RedirectToAction(nameof(Index), new { status = NormalizeStatus(status), academicYearId, page = Math.Max(1, page) });
        }

        var adminIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(adminIdClaim, out var adminId))
        {
            return Challenge();
        }

        try
        {
            await _enrollmentService.ReassignSectionAsync(id, new ReassignSectionRequest
            {
                NewSectionId = form.NewSectionId,
                Reason = string.IsNullOrWhiteSpace(form.Reason) ? null : form.Reason.Trim()
            }, adminId);

            TempData["EnrollmentsSuccess"] = "Enrollment reassigned successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reassign enrollment {EnrollmentId}.", id);
            TempData["EnrollmentsError"] = "Unable to reassign enrollment right now.";
        }

        return RedirectToAction(nameof(Index), new { status = NormalizeStatus(status), academicYearId, page = Math.Max(1, page) });
    }

    private async Task<EnrollmentsIndexViewModel> BuildIndexViewModelAsync(
        string role,
        string? status,
        int? academicYearId,
        int page,
        int pageSize)
    {
        var model = new EnrollmentsIndexViewModel
        {
            IsAdmin = role.IsRole(UserRole.Admin),
            IsStudent = role.IsRole(UserRole.Student),
            SelectedStatus = status,
            SelectedAcademicYearId = academicYearId,
            Page = page,
            PageSize = pageSize
        };

        await PopulateAcademicYearOptionsAsync(model);

        if (model.IsAdmin)
        {
            await PopulateSectionOptionsAsync(model);
            await PopulateAdminDataAsync(model);
        }

        if (model.IsStudent)
        {
            await PopulateCourseOptionsAsync(model);
            await PopulateStudentDataAsync(model);
        }

        return model;
    }

    private async Task PopulateAcademicYearOptionsAsync(EnrollmentsIndexViewModel model)
    {
        try
        {
            var academicYears = await _academicYearsService.GetAllAcademicYearsAsync();

            model.AcademicYears = academicYears
                .OrderByDescending(y => y.StartDate)
                .Select(year => new EnrollmentOptionViewModel
                {
                    Id = year.Id,
                    Label = year.YearLabel
                })
                .ToList();

            if (model.IsStudent)
            {
                var defaultAcademicYearId = model.SelectedAcademicYearId
                    ?? academicYears.FirstOrDefault(y => y.IsActive)?.Id
                    ?? academicYears.OrderByDescending(y => y.StartDate).Select(y => (int?)y.Id).FirstOrDefault();

                if (defaultAcademicYearId.HasValue)
                {
                    model.CreateForm.AcademicYearId = defaultAcademicYearId.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load academic years for enrollments view.");
            model.ErrorMessage ??= "Unable to load academic periods right now.";
        }
    }

    private async Task PopulateSectionOptionsAsync(EnrollmentsIndexViewModel model)
    {
        try
        {
            var sections = await _sectionsService.GetAllSectionsAsync();

            model.Sections = sections
                .OrderBy(s => s.Name)
                .Select(section => new EnrollmentOptionViewModel
                {
                    Id = section.Id,
                    CourseId = section.CourseId,
                    Label = $"{section.Name} | Year {section.YearLevel}"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section options for enrollments view.");
            model.ErrorMessage ??= "Unable to load section options right now.";
        }
    }

    private async Task PopulateCourseOptionsAsync(EnrollmentsIndexViewModel model)
    {
        try
        {
            var courses = await _coursesService.GetAllCoursesAsync();

            model.Courses = courses
                .OrderBy(c => c.Name)
                .Select(course => new EnrollmentOptionViewModel
                {
                    Id = course.Id,
                    EducationLevel = course.EducationLevel,
                    EducationLevelLabel = EducationLevelPolicy.ToDisplayLabel(course.EducationLevel),
                    MinYearLevel = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel).MinYearLevel,
                    MaxYearLevel = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel).MaxYearLevel,
                    Label = string.IsNullOrWhiteSpace(course.Code)
                        ? course.Name
                        : $"{course.Code} - {course.Name}"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load courses for enrollments view.");
            model.ErrorMessage ??= "Unable to load course options right now.";
        }
    }

    private async Task PopulateAdminDataAsync(EnrollmentsIndexViewModel model)
    {
        try
        {
            var list = await _enrollmentService.GetAllEnrollmentsAsync(
                model.SelectedStatus,
                model.SelectedAcademicYearId,
                model.Page,
                model.PageSize);

            model.TotalCount = list.TotalCount;
            model.PendingCount = list.PendingCount;
            model.ApprovedCount = list.ApprovedCount;
            model.RejectedCount = list.RejectedCount;

            model.Enrollments = list.Enrollments
                .OrderByDescending(enrollment => enrollment.CreatedAt)
                .Select(enrollment => new EnrollmentListItemViewModel
                {
                    Id = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = string.IsNullOrWhiteSpace(enrollment.StudentName) ? "-" : enrollment.StudentName,
                    StudentNumber = string.IsNullOrWhiteSpace(enrollment.StudentNumber) ? "-" : enrollment.StudentNumber,
                    StudentCourseId = enrollment.StudentCourseId,
                    SectionId = enrollment.SectionId,
                    SectionName = string.IsNullOrWhiteSpace(enrollment.SectionName) ? "-" : enrollment.SectionName,
                    AcademicYearId = enrollment.AcademicYearId,
                    AcademicYearLabel = string.IsNullOrWhiteSpace(enrollment.AcademicYearLabel) ? "-" : enrollment.AcademicYearLabel,
                    Status = string.IsNullOrWhiteSpace(enrollment.Status) ? EnrollmentStatus.Pending.ToStorageValue() : enrollment.Status,
                    CreatedAt = enrollment.CreatedAt,
                    ProcessedAt = enrollment.ProcessedAt,
                    ProcessorName = string.IsNullOrWhiteSpace(enrollment.ProcessorName) ? "-" : enrollment.ProcessorName,
                    RejectionReason = enrollment.RejectionReason,
                    HasWarning = enrollment.HasWarning,
                    WarningMessage = enrollment.WarningMessage
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load enrollments for admin view.");
            model.ErrorMessage ??= "Unable to load enrollments right now.";
        }
    }

    private async Task PopulateStudentDataAsync(EnrollmentsIndexViewModel model)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            model.ErrorMessage ??= "Unable to identify the signed-in student.";
            return;
        }

        try
        {
            var profile = await _studentsService.GetMyProfileAsync(userId);

            var fullName = string.Join(" ", new[] { profile.FirstName, profile.MiddleName, profile.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            var courseText = string.IsNullOrWhiteSpace(profile.CourseCode) && string.IsNullOrWhiteSpace(profile.CourseName)
                ? "Not assigned"
                : $"{profile.CourseCode} {profile.CourseName}".Trim();

            if (profile.CourseId > 0)
            {
                model.CreateForm.CourseId = profile.CourseId;
            }
            else if (model.CreateForm.CourseId <= 0 && model.Courses.Count > 0)
            {
                model.CreateForm.CourseId = model.Courses[0].Id;
            }

            var selectedCourseOption = model.CreateForm.CourseId > 0
                ? model.Courses.FirstOrDefault(course => course.Id == model.CreateForm.CourseId)
                : null;

            var minYearLevel = selectedCourseOption?.MinYearLevel ?? 1;
            var maxYearLevel = selectedCourseOption?.MaxYearLevel ?? 12;

            var profileYearLevel = profile.YearLevel > 0 ? profile.YearLevel : minYearLevel;
            if (profileYearLevel < minYearLevel || profileYearLevel > maxYearLevel)
            {
                profileYearLevel = minYearLevel;
            }

            model.StudentProfile = new StudentEnrollmentProfileViewModel
            {
                StudentNumber = profile.StudentNumber,
                FullName = fullName,
                YearLevel = profileYearLevel,
                CourseId = profile.CourseId > 0 ? profile.CourseId : null,
                EducationLevel = selectedCourseOption?.EducationLevel,
                MinYearLevel = minYearLevel,
                MaxYearLevel = maxYearLevel,
                CourseText = courseText
            };

            if (model.CreateForm.AcademicYearId <= 0)
            {
                model.CreateForm.AcademicYearId = model.SelectedAcademicYearId ?? model.AcademicYears.FirstOrDefault()?.Id ?? 0;
            }

            if (model.CreateForm.YearLevel <= 0)
            {
                model.CreateForm.YearLevel = profileYearLevel;
            }

            if (model.CreateForm.YearLevel < minYearLevel || model.CreateForm.YearLevel > maxYearLevel)
            {
                model.CreateForm.YearLevel = minYearLevel;
            }

            if (model.CreateForm.CourseId <= 0 || model.CreateForm.YearLevel <= 0 || model.CreateForm.AcademicYearId <= 0)
            {
                return;
            }

            try
            {
                var sections = await _enrollmentService.GetAvailableSectionsForStudentAsync(
                    model.CreateForm.CourseId,
                    model.CreateForm.YearLevel,
                    model.CreateForm.AcademicYearId);

                model.AvailableSections = sections
                    .OrderBy(section => section.Status)
                    .ThenBy(section => section.SectionName)
                    .Select(section => new SectionCapacityItemViewModel
                    {
                        SectionId = section.SectionId,
                        SectionName = section.SectionName,
                        YearLevel = section.YearLevel,
                        CurrentEnrollment = section.CurrentEnrollment,
                        AvailableSlots = section.AvailableSlots,
                        Status = section.Status
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load available sections for student enrollment.");
                model.ErrorMessage ??= "Unable to load available sections right now.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load student profile for user {UserId}.", userId);
            model.ErrorMessage ??= "Unable to load student profile right now.";
        }
    }

    private static string? NormalizeStatus(string? status)
    {
        return EnumStorage.TryParseEnrollmentStatus(status, out var parsedStatus)
            ? parsedStatus.ToStorageValue()
            : null;
    }
}