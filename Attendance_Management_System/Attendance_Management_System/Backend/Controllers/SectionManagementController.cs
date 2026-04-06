using System.Security.Claims;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ValueObjects;
using Attendance_Management_System.Backend.ViewModels.Sections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("sections")]
public class SectionManagementController : Controller
{
    private readonly ISectionsService _sectionsService;
    private readonly ISchedulesService _schedulesService;
    private readonly IStudentsService _studentsService;
    private readonly IAttendanceService _attendanceService;
    private readonly ITeachersService _teachersService;
    private readonly IAcademicYearsService _academicYearsService;
    private readonly ICoursesService _coursesService;
    private readonly ISubjectsService _subjectsService;
    private readonly IClassroomsService _classroomsService;

    private static readonly int[] TimetableDayOrder = { 1, 2, 3, 4, 5, 6, 0 };
    private static readonly Dictionary<int, string> TimetableDayNames = new()
    {
        [0] = "Sunday",
        [1] = "Monday",
        [2] = "Tuesday",
        [3] = "Wednesday",
        [4] = "Thursday",
        [5] = "Friday",
        [6] = "Saturday"
    };

    private static readonly TimeOnly TimetableStart = new(5, 0);
    private static readonly TimeOnly TimetableEnd = new(19, 0);

    public SectionManagementController(
        ISectionsService sectionsService,
        ISchedulesService schedulesService,
        IStudentsService studentsService,
        IAttendanceService attendanceService,
        ITeachersService teachersService,
        IAcademicYearsService academicYearsService,
        ICoursesService coursesService,
        ISubjectsService subjectsService,
        IClassroomsService classroomsService)
    {
        _sectionsService = sectionsService;
        _schedulesService = schedulesService;
        _studentsService = studentsService;
        _attendanceService = attendanceService;
        _teachersService = teachersService;
        _academicYearsService = academicYearsService;
        _coursesService = coursesService;
        _subjectsService = subjectsService;
        _classroomsService = classroomsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? sectionId, [FromQuery] int? scheduleId, [FromQuery] DateOnly? attendanceDate)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var viewModel = await BuildIndexViewModelAsync(context.UserId, context.Role, sectionId, scheduleId, attendanceDate);
        return View(viewModel);
    }

    [HttpPost("create")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateSectionFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var viewModel = await BuildIndexViewModelAsync(context.UserId, context.Role, null, null, null);
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var result = await _sectionsService.CreateSectionAsync(new CreateSectionRequest
        {
            Name = form.Name.Trim(),
            YearLevel = form.YearLevel,
            AcademicYearId = form.AcademicYearId,
            CourseId = form.CourseId,
            SubjectId = form.SubjectId,
            ClassroomId = form.ClassroomId
        });

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.Name", result.Error?.Message ?? "Unable to create section right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["SectionsSuccess"] = "Section created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateSectionFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SectionsError"] = "Please provide valid section details.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _sectionsService.UpdateSectionAsync(id, new UpdateSectionRequest
        {
            Name = form.Name.Trim(),
            YearLevel = form.YearLevel
        });

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to update section right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Section updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _sectionsService.DeleteSectionAsync(id);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to delete section right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Section deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/teachers/assign")]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignTeacher(int id, [Bind(Prefix = "AssignForm")] AssignSectionTeacherFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["SectionsError"] = "Please select a valid teacher.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _sectionsService.AssignTeacherToSectionAsync(id, new AssignTeacherRequest
        {
            TeacherId = form.TeacherId
        });

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to assign teacher right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Teacher assigned successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/teachers/self-assign")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelfAssignTeacher(int id, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var teacherContext = await BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContext.Success || !teacherContext.Context.TeacherId.HasValue)
        {
            TempData["SectionsError"] = teacherContext.Error ?? "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        var teacherId = teacherContext.Context.TeacherId.Value;

        var sectionTeachersResult = await _sectionsService.GetSectionTeachersAsync(id);
        if (sectionTeachersResult.Success && sectionTeachersResult.Data is not null
            && sectionTeachersResult.Data.Any(assignment => assignment.TeacherId == teacherId))
        {
            TempData["SectionsSuccess"] = "You are already assigned to this section.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        var assignResult = await _sectionsService.AssignTeacherToSectionAsync(id, new AssignTeacherRequest
        {
            TeacherId = teacherId
        });

        if (!assignResult.Success)
        {
            TempData["SectionsError"] = assignResult.Error?.Message ?? "Unable to self-assign to this section right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        TempData["SectionsSuccess"] = "You are now assigned to this section.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
    }

    [HttpPost("{id:int}/teachers/self-unassign")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelfUnassignTeacher(int id, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var teacherContext = await BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContext.Success || !teacherContext.Context.TeacherId.HasValue)
        {
            TempData["SectionsError"] = teacherContext.Error ?? "Teacher profile not found for the current account.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        var teacherId = teacherContext.Context.TeacherId.Value;

        var sectionTeachersResult = await _sectionsService.GetSectionTeachersAsync(id);
        if (!sectionTeachersResult.Success || sectionTeachersResult.Data is null)
        {
            TempData["SectionsError"] = sectionTeachersResult.Error?.Message ?? "Unable to load section teacher assignments right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        if (!sectionTeachersResult.Data.Any(assignment => assignment.TeacherId == teacherId))
        {
            TempData["SectionsSuccess"] = "You are not assigned to this section.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        // Self-unassign also removes schedules owned by this teacher in the section.
        var removeResult = await _sectionsService.RemoveTeacherFromSectionAsync(
            id,
            teacherId,
            isAdmin: true,
            removeOwnedSchedules: true);
        if (!removeResult.Success)
        {
            TempData["SectionsError"] = removeResult.Error?.Message ?? "Unable to unassign from this section right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
        }

        TempData["SectionsSuccess"] = "You are no longer assigned to this section and your schedules were removed.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? id });
    }

    [HttpPost("{id:int}/teachers/remove")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeacher(int id, int teacherId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var isAdmin = role == "admin";

        var result = await _sectionsService.RemoveTeacherFromSectionAsync(id, teacherId, isAdmin);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to remove teacher assignment right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SectionsSuccess"] = "Teacher removed from section successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("timetable/slots/add")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimetableSlot(int sectionId, int subjectId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (endTime <= startTime)
        {
            TempData["SectionsError"] = "End time must be after start time.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var subjectSelectionValidation = await ValidateSubjectSelectionForSectionAsync(sectionId, subjectId);
        if (!subjectSelectionValidation.IsValid)
        {
            TempData["SectionsError"] = subjectSelectionValidation.ErrorMessage;
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var result = await _schedulesService.CreateScheduleAsync(new CreateScheduleRequest
        {
            SectionId = sectionId,
            SubjectId = subjectId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.Today)
        }, context.UserId);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to add the timetable slot right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        TempData["SectionsSuccess"] = "Timetable slot added successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
    }

    [HttpPost("timetable/slots/add-range")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTimetableSlotRange(int sectionId, int subjectId, TimeOnly startTime, TimeOnly endTime, [FromForm] List<int>? selectedDays, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (endTime <= startTime)
        {
            TempData["SectionsError"] = "End time must be after start time.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var normalizedDays = (selectedDays ?? [])
            .Where(day => day >= 0 && day <= 6)
            .Distinct()
            .OrderBy(day => day)
            .ToList();

        if (normalizedDays.Count == 0)
        {
            TempData["SectionsError"] = "Select at least one weekday.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var subjectSelectionValidation = await ValidateSubjectSelectionForSectionAsync(sectionId, subjectId);
        if (!subjectSelectionValidation.IsValid)
        {
            TempData["SectionsError"] = subjectSelectionValidation.ErrorMessage;
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var result = await _schedulesService.CreateScheduleRangeAsync(new CreateScheduleRangeRequest
        {
            SectionId = sectionId,
            SubjectId = subjectId,
            DaysOfWeek = normalizedDays,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.Today)
        }, context.UserId);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to add timetable slots right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
        }

        var createdCount = result.Data?.Count ?? normalizedDays.Count;
        TempData["SectionsSuccess"] = createdCount == 1
            ? "Timetable slot added successfully."
            : $"Timetable slots added successfully ({createdCount} days).";

        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId ?? sectionId });
    }

    [HttpPost("timetable/slots/{scheduleId:int}/update")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTimetableSlot(int scheduleId, int subjectId, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (endTime <= startTime)
        {
            TempData["SectionsError"] = "End time must be after start time.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        if (subjectId <= 0)
        {
            TempData["SectionsError"] = "Select a subject before saving the timetable slot.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        var result = await _schedulesService.UpdateScheduleAsync(scheduleId, new UpdateScheduleRequest
        {
            SubjectId = subjectId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime
        }, context.UserId);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to update the timetable slot right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        TempData["SectionsSuccess"] = "Timetable slot updated successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
    }

    [HttpPost("timetable/slots/{scheduleId:int}/delete")]
    [Authorize(Policy = "TeacherOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTimetableSlot(int scheduleId, int? selectedSectionId)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        var result = await _schedulesService.DeleteScheduleAsync(scheduleId, context.UserId, isAdmin: false);

        if (!result.Success)
        {
            TempData["SectionsError"] = result.Error?.Message ?? "Unable to delete the timetable slot right now.";
            return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
        }

        TempData["SectionsSuccess"] = "Timetable slot deleted successfully.";
        return RedirectToAction(nameof(Index), new { sectionId = selectedSectionId });
    }

    [HttpPost("attendance/mark")]
    [Authorize(Policy = "AdminOrTeacher")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSectionAttendance([FromForm] SectionMarkAttendanceFormViewModel form)
    {
        var context = GetUserContext();
        if (!context.IsValid)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            TempData["SectionAttendanceError"] = "Please provide valid attendance details.";
            return RedirectToAction(nameof(Index), new
            {
                sectionId = form.SectionId,
                scheduleId = form.ScheduleId,
                attendanceDate = form.Date.ToString("yyyy-MM-dd")
            });
        }

        var teacherContextResult = await BuildTeacherContextAsync(context.UserId, context.Role);
        if (!teacherContextResult.Success)
        {
            TempData["SectionAttendanceError"] = teacherContextResult.Error ?? "Unable to identify teacher context.";
            return RedirectToAction(nameof(Index), new
            {
                sectionId = form.SectionId,
                scheduleId = form.ScheduleId,
                attendanceDate = form.Date.ToString("yyyy-MM-dd")
            });
        }

        var result = await _attendanceService.MarkAttendanceAsync(new MarkAttendanceRequest
        {
            SectionId = form.SectionId,
            ScheduleId = form.ScheduleId,
            StudentId = form.StudentId,
            Date = form.Date,
            TimeIn = form.TimeIn,
            Remarks = NormalizeOptional(form.Remarks)
        }, teacherContextResult.Context);

        if (!result.Success)
        {
            TempData["SectionAttendanceError"] = result.Error?.Message ?? "Unable to mark attendance right now.";
            return RedirectToAction(nameof(Index), new
            {
                sectionId = form.SectionId,
                scheduleId = form.ScheduleId,
                attendanceDate = form.Date.ToString("yyyy-MM-dd")
            });
        }

        TempData["SectionAttendanceSuccess"] = "Attendance marked successfully.";
        return RedirectToAction(nameof(Index), new
        {
            sectionId = form.SectionId,
            scheduleId = form.ScheduleId,
            attendanceDate = form.Date.ToString("yyyy-MM-dd")
        });
    }

    private async Task<SectionsIndexViewModel> BuildIndexViewModelAsync(
        int currentUserId,
        string role,
        int? requestedSectionId,
        int? requestedScheduleId,
        DateOnly? requestedAttendanceDate)
    {
        var result = await _sectionsService.GetAllSectionsAsync();

        var viewModel = new SectionsIndexViewModel
        {
            IsAdmin = role == "admin",
            IsTeacher = role == "teacher",
            SelectedAttendanceDate = requestedAttendanceDate ?? DateOnly.FromDateTime(DateTime.Today)
        };

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load sections right now.";
            return viewModel;
        }

        viewModel.Sections = result.Data
            .OrderBy(s => s.Name)
            .Select(section => new SectionListItemViewModel
            {
                Id = section.Id,
                Name = section.Name,
                YearLevel = section.YearLevel,
                CourseName = string.IsNullOrWhiteSpace(section.CourseName) ? "-" : section.CourseName,
                SubjectName = string.IsNullOrWhiteSpace(section.SubjectName) ? "-" : section.SubjectName,
                ClassroomName = string.IsNullOrWhiteSpace(section.ClassroomName) ? "-" : section.ClassroomName,
                CurrentEnrollmentCount = section.CurrentEnrollmentCount
            })
            .ToList();

        viewModel.SectionOptions = viewModel.Sections
            .Select(section => new SectionOptionViewModel
            {
                Id = section.Id,
                Name = section.Name
            })
            .ToList();

        await PopulateCreateSectionOptionsAsync(viewModel);
        await PopulateTeacherOptionsAsync(viewModel);

        if (!viewModel.SectionOptions.Any())
        {
            return viewModel;
        }

        var selectedSectionId = requestedSectionId.HasValue && viewModel.SectionOptions.Any(s => s.Id == requestedSectionId.Value)
            ? requestedSectionId.Value
            : viewModel.SectionOptions.First().Id;

        viewModel.SelectedSectionId = selectedSectionId;

        var selectedSection = result.Data.FirstOrDefault(s => s.Id == selectedSectionId);
        if (selectedSection != null)
        {
            viewModel.SelectedSectionName = selectedSection.Name;
            viewModel.SelectedSectionSubjectId = selectedSection.SubjectId;
            viewModel.SelectedSectionSubjectName = string.IsNullOrWhiteSpace(selectedSection.SubjectName)
                ? "-"
                : selectedSection.SubjectName;
        }

        await PopulateTimetableSubjectOptionsAsync(viewModel, selectedSection);
        await PopulateTeacherAssignmentStateAsync(viewModel, currentUserId, role, selectedSectionId);

        var timetableResult = await _sectionsService.GetTimetableAsync(selectedSectionId, currentUserId);
        if (!timetableResult.Success || timetableResult.Data is null)
        {
            viewModel.TimetableErrorMessage = timetableResult.Error?.Message ?? "Unable to load timetable right now.";
        }

        if (timetableResult.Success && timetableResult.Data is not null)
        {
            var slots = new List<TimetableSlotRecord>();
            foreach (var dayEntry in timetableResult.Data.Timetable)
            {
                foreach (var slot in dayEntry.Value)
                {
                    if (!TimeOnly.TryParse(slot.StartTime, out var parsedStart) || !TimeOnly.TryParse(slot.EndTime, out var parsedEnd))
                    {
                        continue;
                    }

                    var dayOfWeek = slot.DayOfWeek >= 0 && slot.DayOfWeek <= 6
                        ? slot.DayOfWeek
                        : ResolveDayOfWeek(dayEntry.Key);

                    slots.Add(new TimetableSlotRecord
                    {
                        ScheduleId = slot.ScheduleId,
                        SubjectId = slot.SubjectId,
                        SubjectName = slot.SubjectName,
                        TeacherName = string.IsNullOrWhiteSpace(slot.TeacherName) ? "Unassigned" : slot.TeacherName,
                        DayOfWeek = dayOfWeek,
                        StartTime = parsedStart,
                        EndTime = parsedEnd,
                        IsMine = slot.IsMine
                    });
                }
            }

            viewModel.TimetableRows = BuildTimetableRows(slots);
        }

        await PopulateAttendancePanelAsync(viewModel, currentUserId, role, selectedSectionId, requestedScheduleId);

        return viewModel;
    }

    private async Task PopulateAttendancePanelAsync(
        SectionsIndexViewModel viewModel,
        int currentUserId,
        string role,
        int selectedSectionId,
        int? requestedScheduleId)
    {
        var schedulesResult = await _schedulesService.GetSchedulesAsync(currentUserId, role);
        if (!schedulesResult.Success || schedulesResult.Data is null)
        {
            viewModel.AttendanceErrorMessage = schedulesResult.Error?.Message ?? "Unable to load schedules for attendance.";
            return;
        }

        var allowedSchedules = schedulesResult.Data
            .Where(schedule => schedule.SectionId == selectedSectionId);

        if (string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase))
        {
            allowedSchedules = allowedSchedules.Where(schedule => schedule.IsMine);
        }

        var scheduleOptions = allowedSchedules
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule => new SectionAttendanceScheduleOptionViewModel
            {
                Id = schedule.Id,
                SectionId = schedule.SectionId,
                Label = $"{schedule.SubjectName} | {schedule.DayName} {schedule.StartTime}-{schedule.EndTime}"
            })
            .ToList();

        viewModel.AttendanceSchedules = scheduleOptions;

        if (!scheduleOptions.Any())
        {
            return;
        }

        var selectedScheduleId = requestedScheduleId.HasValue && scheduleOptions.Any(schedule => schedule.Id == requestedScheduleId.Value)
            ? requestedScheduleId
            : scheduleOptions.First().Id;

        viewModel.SelectedAttendanceScheduleId = selectedScheduleId;
        if (!selectedScheduleId.HasValue)
        {
            return;
        }

        var studentsResult = await _studentsService.GetStudentsBySectionAsync(selectedSectionId, currentUserId, role);
        if (!studentsResult.Success || studentsResult.Data is null)
        {
            viewModel.AttendanceErrorMessage = studentsResult.Error?.Message ?? "Unable to load students for the selected section.";
            return;
        }

        var summaryResult = await _attendanceService.GetSectionAttendanceAsync(
            selectedSectionId,
            viewModel.SelectedAttendanceDate,
            selectedScheduleId.Value);

        if (!summaryResult.Success || summaryResult.Data is null)
        {
            viewModel.AttendanceErrorMessage = summaryResult.Error?.Message ?? "Unable to load attendance summary.";
            return;
        }

        viewModel.AttendanceTotalStudents = summaryResult.Data.TotalStudents;
        viewModel.AttendancePresentCount = summaryResult.Data.PresentCount;
        viewModel.AttendanceLateCount = summaryResult.Data.LateCount;
        viewModel.AttendanceAbsentCount = summaryResult.Data.AbsentCount;
        viewModel.AttendanceUnmarkedCount = summaryResult.Data.UnmarkedCount;

        var recordsByStudentId = summaryResult.Data.Records
            .GroupBy(record => record.StudentId)
            .ToDictionary(group => group.Key, group => group.First());

        viewModel.AttendanceStudents = studentsResult.Data
            .OrderBy(student => student.LastName)
            .ThenBy(student => student.FirstName)
            .Select(student =>
            {
                recordsByStudentId.TryGetValue(student.Id, out var record);
                return BuildAttendanceStudentRow(student, record);
            })
            .ToList();
    }

    private static SectionAttendanceStudentRowViewModel BuildAttendanceStudentRow(StudentBasicProfileDto student, AttendanceDto? record)
    {
        var fullName = string.Join(" ", new[] { student.FirstName, student.MiddleName, student.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        var courseText = string.IsNullOrWhiteSpace(student.CourseCode) && string.IsNullOrWhiteSpace(student.CourseName)
            ? "-"
            : $"{student.CourseCode} {student.CourseName}".Trim();

        var hasRecord = record is not null;
        var isMarked = hasRecord && record!.IsMarked;
        var statusLabel = hasRecord ? record!.StatusLabel : "Unmarked";
        var statusClass = hasRecord ? record!.StatusClass : "inactive";

        return new SectionAttendanceStudentRowViewModel
        {
            StudentId = student.Id,
            StudentNumber = string.IsNullOrWhiteSpace(student.StudentNumber) ? "-" : student.StudentNumber,
            FullName = string.IsNullOrWhiteSpace(fullName) ? "-" : fullName,
            YearLevel = student.YearLevel,
            CourseText = courseText,
            IsMarked = isMarked,
            StatusLabel = statusLabel,
            StatusClass = statusClass,
            ExistingTimeIn = isMarked ? record!.TimeIn?.ToString("HH:mm") ?? "-" : "-",
            ExistingTimeInValue = isMarked ? record!.TimeIn?.ToString("HH:mm") ?? string.Empty : string.Empty,
            ExistingRemarks = isMarked
                ? string.IsNullOrWhiteSpace(record!.Remarks) ? "-" : record.Remarks!
                : "-",
            EditableRemarksValue = isMarked ? record!.Remarks ?? string.Empty : string.Empty,
            MarkerName = isMarked
                ? string.IsNullOrWhiteSpace(record!.MarkerName) ? "-" : record.MarkerName!
                : "-",
            ActionLabel = isMarked ? "Save Correction" : "Mark"
        };
    }

    private async Task PopulateCreateSectionOptionsAsync(SectionsIndexViewModel viewModel)
    {
        if (!viewModel.IsAdmin)
        {
            return;
        }

        // These lookups share a request-scoped DbContext under the hood.
        // Run them sequentially to avoid concurrent DbContext operations.
        var academicPeriodsResult = await _academicYearsService.GetAllAcademicYearsAsync();
        var coursesResult = await _coursesService.GetAllCoursesAsync();
        var subjectsResult = await _subjectsService.GetAllSubjectsAsync();
        var classroomsResult = await _classroomsService.GetAllClassroomsAsync();

        var lookupLoadFailures = new List<string>();

        if (!academicPeriodsResult.Success || academicPeriodsResult.Data is null)
        {
            lookupLoadFailures.Add("academic periods");
        }
        else
        {
            viewModel.AcademicPeriods = academicPeriodsResult.Data
                .OrderByDescending(period => period.StartDate)
                .Select(period => new SectionReferenceOptionViewModel
                {
                    Id = period.Id,
                    Label = period.YearLabel
                })
                .ToList();
        }

        if (!coursesResult.Success || coursesResult.Data is null)
        {
            lookupLoadFailures.Add("courses");
        }
        else
        {
            viewModel.Courses = coursesResult.Data
                .OrderBy(course => course.Name)
                .Select(course => new SectionReferenceOptionViewModel
                {
                    Id = course.Id,
                    Label = string.IsNullOrWhiteSpace(course.Code)
                        ? course.Name
                        : $"{course.Code} - {course.Name}"
                })
                .ToList();
        }

        if (!subjectsResult.Success || subjectsResult.Data is null)
        {
            lookupLoadFailures.Add("subjects");
        }
        else
        {
            viewModel.Subjects = subjectsResult.Data
                .OrderBy(subject => subject.Name)
                .Select(subject => new SectionSubjectReferenceOptionViewModel
                {
                    Id = subject.Id,
                    CourseId = subject.CourseId,
                    Label = BuildSubjectOptionLabel(subject)
                })
                .ToList();
        }

        if (!classroomsResult.Success || classroomsResult.Data is null)
        {
            lookupLoadFailures.Add("classrooms");
        }
        else
        {
            viewModel.Classrooms = classroomsResult.Data
                .OrderBy(classroom => classroom.Name)
                .Select(classroom => new SectionReferenceOptionViewModel
                {
                    Id = classroom.Id,
                    Label = classroom.Name
                })
                .ToList();
        }

        if (lookupLoadFailures.Count == 0)
        {
            return;
        }

        viewModel.CreateSectionOptionsErrorMessage =
            $"Some create form options could not be loaded ({string.Join(", ", lookupLoadFailures)}).";
    }

    private async Task PopulateTimetableSubjectOptionsAsync(SectionsIndexViewModel viewModel, SectionDto? selectedSection)
    {
        viewModel.TimetableSubjects = [];
        viewModel.TimetableSubjectsErrorMessage = null;

        if (selectedSection is null)
        {
            return;
        }

        var subjectsResult = await _subjectsService.GetAllSubjectsAsync();
        if (!subjectsResult.Success || subjectsResult.Data is null)
        {
            viewModel.TimetableSubjectsErrorMessage = subjectsResult.Error?.Message ?? "Unable to load timetable subject options right now.";
            return;
        }

        viewModel.TimetableSubjects = subjectsResult.Data
            .OrderBy(subject => subject.Name)
            .Select(subject => new SectionReferenceOptionViewModel
            {
                Id = subject.Id,
                Label = BuildSubjectOptionLabel(subject)
            })
            .ToList();
    }

    private async Task PopulateTeacherAssignmentStateAsync(SectionsIndexViewModel viewModel, int currentUserId, string role, int selectedSectionId)
    {
        viewModel.IsCurrentTeacherAssignedToSelectedSection = false;

        if (!string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var teachersResult = await _teachersService.GetAllTeachersAsync();
        if (!teachersResult.Success || teachersResult.Data is null)
        {
            return;
        }

        var teacherId = teachersResult.Data
            .FirstOrDefault(teacher => teacher.UserId == currentUserId)
            ?.Id;

        if (!teacherId.HasValue)
        {
            return;
        }

        var sectionTeachersResult = await _sectionsService.GetSectionTeachersAsync(selectedSectionId);
        if (!sectionTeachersResult.Success || sectionTeachersResult.Data is null)
        {
            return;
        }

        viewModel.IsCurrentTeacherAssignedToSelectedSection = sectionTeachersResult.Data
            .Any(assignment => assignment.TeacherId == teacherId.Value);
    }

    private async Task PopulateTeacherOptionsAsync(SectionsIndexViewModel viewModel)
    {
        if (!viewModel.IsAdmin)
        {
            return;
        }

        var teachersResult = await _teachersService.GetAllTeachersWithSectionsAsync();
        if (!teachersResult.Success || teachersResult.Data is null)
        {
            viewModel.TeacherOptionsErrorMessage = teachersResult.Error?.Message ?? "Unable to load teacher options right now.";
            return;
        }

        var activeTeachers = teachersResult.Data
            .Where(teacher => teacher.IsActive)
            .OrderBy(teacher => teacher.LastName)
            .ThenBy(teacher => teacher.FirstName)
            .ToList();

        viewModel.TeacherOptions = activeTeachers
            .Select(teacher => new SectionTeacherOptionViewModel
            {
                Id = teacher.Id,
                Label = BuildTeacherOptionLabel(teacher),
                ShortLabel = BuildTeacherShortLabel(teacher)
            })
            .ToList();

        var assignedTeacherLookup = activeTeachers
            .SelectMany(teacher => teacher.Sections.Select(section => new { section.SectionId, Teacher = teacher }))
            .GroupBy(entry => entry.SectionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<SectionTeacherOptionViewModel>)group
                    .OrderBy(entry => entry.Teacher.LastName)
                    .ThenBy(entry => entry.Teacher.FirstName)
                    .Select(entry => new SectionTeacherOptionViewModel
                    {
                        Id = entry.Teacher.Id,
                        Label = BuildTeacherOptionLabel(entry.Teacher),
                        ShortLabel = BuildTeacherShortLabel(entry.Teacher)
                    })
                    .ToList());

        foreach (var section in viewModel.Sections)
        {
            if (assignedTeacherLookup.TryGetValue(section.Id, out var assignedTeachers))
            {
                section.AssignedTeachers = assignedTeachers;
                section.AssignedTeacherSummary = string.Join(", ", assignedTeachers.Select(teacher => teacher.ShortLabel));
                continue;
            }

            section.AssignedTeachers = [];
            section.AssignedTeacherSummary = "No teacher assigned";
        }
    }

    private static string BuildTeacherOptionLabel(TeacherListDto teacher)
    {
        var fullName = BuildTeacherFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName);
        var employeeNumber = string.IsNullOrWhiteSpace(teacher.EmployeeNumber)
            ? "No employee number"
            : teacher.EmployeeNumber.Trim();
        var department = string.IsNullOrWhiteSpace(teacher.Department)
            ? "No department"
            : teacher.Department.Trim();

        return $"{fullName} ({employeeNumber}) - {department}";
    }

    private static string BuildTeacherShortLabel(TeacherListDto teacher)
    {
        var fullName = BuildTeacherFullName(teacher.FirstName, teacher.MiddleName, teacher.LastName);
        return string.IsNullOrWhiteSpace(teacher.EmployeeNumber)
            ? fullName
            : $"{fullName} ({teacher.EmployeeNumber.Trim()})";
    }

    private static string BuildTeacherFullName(string? firstName, string? middleName, string? lastName)
    {
        var fullName = string.Join(" ", new[]
        {
            firstName?.Trim(),
            middleName?.Trim(),
            lastName?.Trim()
        }.Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(fullName) ? "Unnamed teacher" : fullName;
    }

    private static string BuildSubjectOptionLabel(SubjectDto subject)
    {
        var baseLabel = string.IsNullOrWhiteSpace(subject.Code)
            ? subject.Name
            : $"{subject.Code} - {subject.Name}";

        return string.IsNullOrWhiteSpace(subject.CourseName)
            ? baseLabel
            : $"{baseLabel} ({subject.CourseName})";
    }

    private async Task<(bool IsValid, string ErrorMessage)> ValidateSubjectSelectionForSectionAsync(int sectionId, int subjectId)
    {
        if (subjectId <= 0)
        {
            return (false, "Select a subject before adding a timetable slot.");
        }

        var sectionResult = await _sectionsService.GetSectionByIdAsync(sectionId);
        if (!sectionResult.Success || sectionResult.Data is null)
        {
            return (false, sectionResult.Error?.Message ?? "Unable to load the selected section.");
        }

        var subjectResult = await _subjectsService.GetSubjectByIdAsync(subjectId);
        if (!subjectResult.Success || subjectResult.Data is null)
        {
            return (false, subjectResult.Error?.Message ?? "Selected subject was not found.");
        }

        return (true, string.Empty);
    }

    private async Task<(bool Success, TeacherContext Context, string? Error)> BuildTeacherContextAsync(int userId, string role)
    {
        var isAdmin = role == "admin";
        if (isAdmin)
        {
            return (true, new TeacherContext { UserId = userId, TeacherId = null, IsAdmin = true }, null);
        }

        var teachersResult = await _teachersService.GetAllTeachersAsync();
        if (!teachersResult.Success || teachersResult.Data is null)
        {
            return (false, default, "Unable to load teacher profile.");
        }

        var teacherId = teachersResult.Data.FirstOrDefault(teacher => teacher.UserId == userId)?.Id;
        if (!teacherId.HasValue)
        {
            return (false, default, "Teacher profile not found for the current account.");
        }

        return (true, new TeacherContext
        {
            UserId = userId,
            TeacherId = teacherId.Value,
            IsAdmin = false
        }, null);
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static IReadOnlyList<SectionTimetableRowViewModel> BuildTimetableRows(List<TimetableSlotRecord> slots)
    {
        var rows = new List<SectionTimetableRowViewModel>();

        for (var slotStart = TimetableStart; slotStart < TimetableEnd; slotStart = slotStart.AddMinutes(30))
        {
            var slotEnd = slotStart.AddMinutes(30);
            var cells = new List<SectionTimetableCellViewModel>();

            foreach (var day in TimetableDayOrder)
            {
                var occupiedSlot = slots
                    .Where(s => s.DayOfWeek == day)
                    .Where(s => s.StartTime < slotEnd && s.EndTime > slotStart)
                    .OrderBy(s => s.StartTime)
                    .ThenBy(s => s.ScheduleId)
                    .FirstOrDefault();

                if (occupiedSlot == null)
                {
                    cells.Add(new SectionTimetableCellViewModel
                    {
                        DayOfWeek = day,
                        DayName = TimetableDayNames[day],
                        IsOccupied = false,
                        StartTime = slotStart.ToString("HH:mm"),
                        EndTime = slotEnd.ToString("HH:mm")
                    });
                    continue;
                }

                cells.Add(new SectionTimetableCellViewModel
                {
                    DayOfWeek = day,
                    DayName = TimetableDayNames[day],
                    IsOccupied = true,
                    IsMine = occupiedSlot.IsMine,
                    IsStart = occupiedSlot.StartTime == slotStart,
                    ScheduleId = occupiedSlot.ScheduleId,
                    SubjectId = occupiedSlot.SubjectId,
                    SubjectName = occupiedSlot.SubjectName,
                    TeacherName = occupiedSlot.TeacherName,
                    StartTime = occupiedSlot.StartTime.ToString("HH:mm"),
                    EndTime = occupiedSlot.EndTime.ToString("HH:mm"),
                    TimeRange = $"{occupiedSlot.StartTime:HH\\:mm}-{occupiedSlot.EndTime:HH\\:mm}"
                });
            }

            rows.Add(new SectionTimetableRowViewModel
            {
                TimeLabel = slotStart.ToString("hh:mm tt"),
                StartTime = slotStart.ToString("HH:mm"),
                EndTime = slotEnd.ToString("HH:mm"),
                Cells = cells
            });
        }

        return rows;
    }

    private static int ResolveDayOfWeek(string day)
    {
        return day.Trim().ToLowerInvariant() switch
        {
            "sun" or "sunday" => 0,
            "mon" or "monday" => 1,
            "tue" or "tuesday" => 2,
            "wed" or "wednesday" => 3,
            "thu" or "thursday" => 4,
            "fri" or "friday" => 5,
            "sat" or "saturday" => 6,
            _ => 0
        };
    }

    private (bool IsValid, int UserId, string Role, bool IsAdmin) GetUserContext()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        if (!int.TryParse(userIdClaim, out var userId) || string.IsNullOrWhiteSpace(role))
        {
            return (false, 0, string.Empty, false);
        }

        return (true, userId, role, role == "admin");
    }

    private sealed class TimetableSlotRecord
    {
        public int ScheduleId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsMine { get; set; }
    }
}