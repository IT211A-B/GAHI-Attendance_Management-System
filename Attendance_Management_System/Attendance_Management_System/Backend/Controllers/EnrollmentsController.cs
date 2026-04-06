using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public EnrollmentsController(
        IEnrollmentService enrollmentService,
        IAcademicYearsService academicYearsService,
        ISectionsService sectionsService,
        ICoursesService coursesService,
        IStudentsService studentsService)
    {
        _enrollmentService = enrollmentService;
        _academicYearsService = academicYearsService;
        _sectionsService = sectionsService;
        _coursesService = coursesService;
        _studentsService = studentsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? status,
        [FromQuery] int? academicYearId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        if (role != "admin" && role != "student")
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
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var viewModel = await BuildIndexViewModelAsync(
            role: "student",
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

        var result = await _enrollmentService.CreateEnrollmentAsync(new CreateEnrollmentRequest
        {
            CourseId = form.CourseId,
            AcademicYearId = form.AcademicYearId
        }, userId.Value);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error?.Message ?? "Unable to submit enrollment request right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["EnrollmentsSuccess"] = result.Data?.Message ?? "Enrollment request submitted successfully.";
        return RedirectToAction(nameof(Index), new { academicYearId = form.AcademicYearId });
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
        var adminId = GetCurrentUserId();
        if (adminId is null)
        {
            return Challenge();
        }

        var result = await _enrollmentService.UpdateEnrollmentStatusAsync(id, new UpdateEnrollmentStatusRequest
        {
            Status = "approved"
        }, adminId.Value);

        if (!result.Success)
        {
            TempData["EnrollmentsError"] = result.Error?.Message ?? "Unable to approve enrollment right now.";
        }
        else
        {
            TempData["EnrollmentsSuccess"] = "Enrollment approved successfully.";
        }

        return RedirectToIndex(status, academicYearId, page);
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
            return RedirectToIndex(status, academicYearId, page);
        }

        var adminId = GetCurrentUserId();
        if (adminId is null)
        {
            return Challenge();
        }

        var result = await _enrollmentService.UpdateEnrollmentStatusAsync(id, new UpdateEnrollmentStatusRequest
        {
            Status = "rejected",
            RejectionReason = form.RejectionReason.Trim()
        }, adminId.Value);

        if (!result.Success)
        {
            TempData["EnrollmentsError"] = result.Error?.Message ?? "Unable to reject enrollment right now.";
        }
        else
        {
            TempData["EnrollmentsSuccess"] = "Enrollment rejected successfully.";
        }

        return RedirectToIndex(status, academicYearId, page);
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
            return RedirectToIndex(status, academicYearId, page);
        }

        var adminId = GetCurrentUserId();
        if (adminId is null)
        {
            return Challenge();
        }

        var result = await _enrollmentService.ReassignSectionAsync(id, new ReassignSectionRequest
        {
            NewSectionId = form.NewSectionId,
            Reason = string.IsNullOrWhiteSpace(form.Reason) ? null : form.Reason.Trim()
        }, adminId.Value);

        if (!result.Success)
        {
            TempData["EnrollmentsError"] = result.Error?.Message ?? "Unable to reassign enrollment right now.";
        }
        else
        {
            TempData["EnrollmentsSuccess"] = "Enrollment reassigned successfully.";
        }

        return RedirectToIndex(status, academicYearId, page);
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
            IsAdmin = role == "admin",
            IsStudent = role == "student",
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
        var result = await _academicYearsService.GetAllAcademicYearsAsync();
        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage ??= result.Error?.Message ?? "Unable to load academic periods right now.";
            return;
        }

        model.AcademicYears = result.Data
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
                ?? result.Data.FirstOrDefault(y => y.IsActive)?.Id
                ?? result.Data.OrderByDescending(y => y.StartDate).Select(y => (int?)y.Id).FirstOrDefault();

            if (defaultAcademicYearId.HasValue)
            {
                model.CreateForm.AcademicYearId = defaultAcademicYearId.Value;
            }
        }
    }

    private async Task PopulateSectionOptionsAsync(EnrollmentsIndexViewModel model)
    {
        var result = await _sectionsService.GetAllSectionsAsync();
        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage ??= result.Error?.Message ?? "Unable to load section options right now.";
            return;
        }

        model.Sections = result.Data
            .OrderBy(s => s.Name)
            .Select(section => new EnrollmentOptionViewModel
            {
                Id = section.Id,
                CourseId = section.CourseId,
                Label = $"{section.Name} | Year {section.YearLevel}"
            })
            .ToList();
    }

    private async Task PopulateCourseOptionsAsync(EnrollmentsIndexViewModel model)
    {
        var result = await _coursesService.GetAllCoursesAsync();
        if (!result.Success || result.Data is null)
        {
            model.ErrorMessage ??= result.Error?.Message ?? "Unable to load course options right now.";
            return;
        }

        model.Courses = result.Data
            .OrderBy(c => c.Name)
            .Select(course => new EnrollmentOptionViewModel
            {
                Id = course.Id,
                Label = string.IsNullOrWhiteSpace(course.Code)
                    ? course.Name
                    : $"{course.Code} - {course.Name}"
            })
            .ToList();
    }

    private async Task PopulateAdminDataAsync(EnrollmentsIndexViewModel model)
    {
        var result = await _enrollmentService.GetAllEnrollmentsAsync(
            model.SelectedStatus,
            model.SelectedAcademicYearId,
            model.Page,
            model.PageSize);

        model.TotalCount = result.TotalCount;
        model.PendingCount = result.PendingCount;
        model.ApprovedCount = result.ApprovedCount;
        model.RejectedCount = result.RejectedCount;

        model.Enrollments = result.Enrollments
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
                Status = string.IsNullOrWhiteSpace(enrollment.Status) ? "pending" : enrollment.Status,
                CreatedAt = enrollment.CreatedAt,
                ProcessedAt = enrollment.ProcessedAt,
                ProcessorName = string.IsNullOrWhiteSpace(enrollment.ProcessorName) ? "-" : enrollment.ProcessorName,
                RejectionReason = enrollment.RejectionReason,
                HasWarning = enrollment.HasWarning,
                WarningMessage = enrollment.WarningMessage
            })
            .ToList();
    }

    private async Task PopulateStudentDataAsync(EnrollmentsIndexViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            model.ErrorMessage ??= "Unable to identify the signed-in student.";
            return;
        }

        var profileResult = await _studentsService.GetMyProfileAsync(userId.Value);
        if (!profileResult.Success || profileResult.Data is null)
        {
            model.ErrorMessage ??= profileResult.Error?.Message ?? "Unable to load student profile right now.";
            return;
        }

        var profile = profileResult.Data;
        var fullName = string.Join(" ", new[] { profile.FirstName, profile.MiddleName, profile.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        var courseText = string.IsNullOrWhiteSpace(profile.CourseCode) && string.IsNullOrWhiteSpace(profile.CourseName)
            ? "Not assigned"
            : $"{profile.CourseCode} {profile.CourseName}".Trim();

        model.StudentProfile = new StudentEnrollmentProfileViewModel
        {
            StudentNumber = profile.StudentNumber,
            FullName = fullName,
            YearLevel = profile.YearLevel,
            CourseId = profile.CourseId > 0 ? profile.CourseId : null,
            CourseText = courseText
        };

        if (profile.CourseId > 0)
        {
            model.CreateForm.CourseId = profile.CourseId;
        }
        else if (model.CreateForm.CourseId <= 0 && model.Courses.Count > 0)
        {
            model.CreateForm.CourseId = model.Courses[0].Id;
        }

        if (model.CreateForm.AcademicYearId <= 0)
        {
            model.CreateForm.AcademicYearId = model.SelectedAcademicYearId ?? model.AcademicYears.FirstOrDefault()?.Id ?? 0;
        }

        if (model.CreateForm.CourseId <= 0 || model.CreateForm.AcademicYearId <= 0)
        {
            return;
        }

        var studentYearLevel = profile.YearLevel > 0 ? profile.YearLevel : 1;

        var sections = await _enrollmentService.GetAvailableSectionsForStudentAsync(
            model.CreateForm.CourseId,
            studentYearLevel,
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

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "pending" => "pending",
            "approved" => "approved",
            "rejected" => "rejected",
            _ => null
        };
    }

    private IActionResult RedirectToIndex(string? status, int? academicYearId, int page)
    {
        return RedirectToAction(nameof(Index), new
        {
            status = NormalizeStatus(status),
            academicYearId,
            page = Math.Max(1, page)
        });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
