using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ValueObjects;
using Attendance_Management_System.Backend.ViewModels.Sections;

namespace Attendance_Management_System.Backend.Services;

public class SectionPageService : ISectionPageService
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

    public SectionPageService(
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

    public async Task<SectionManagementIndexViewModel> BuildSectionManagementIndexViewModelAsync(int currentUserId, string role)
    {
        _ = currentUserId;

        var viewModel = new SectionManagementIndexViewModel
        {
            IsAdmin = role.IsRole(UserRole.Admin),
            IsTeacher = role.IsRole(UserRole.Teacher)
        };

        var sectionsResult = await TryCallAsync(() => _sectionsService.GetAllSectionsAsync());
        if (!sectionsResult.Success || sectionsResult.Data is null)
        {
            viewModel.ErrorMessage = sectionsResult.Error ?? "Unable to load sections right now.";
            return viewModel;
        }

        viewModel.Sections = sectionsResult.Data
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

        await PopulateCreateSectionOptionsAsync(viewModel);
        await PopulateTeacherOptionsAsync(viewModel);

        return viewModel;
    }

    public async Task<TimetableIndexViewModel> BuildTimetableIndexViewModelAsync(int currentUserId, string role, int? requestedSectionId)
    {
        var viewModel = new TimetableIndexViewModel
        {
            IsAdmin = role.IsRole(UserRole.Admin),
            IsTeacher = role.IsRole(UserRole.Teacher)
        };

        var sectionsResult = await TryCallAsync(() => _sectionsService.GetAllSectionsAsync());
        if (!sectionsResult.Success || sectionsResult.Data is null)
        {
            viewModel.ErrorMessage = sectionsResult.Error ?? "Unable to load sections right now.";
            return viewModel;
        }

        var sections = sectionsResult.Data;

        viewModel.SectionOptions = sections
            .OrderBy(section => section.Name)
            .Select(section => new SectionOptionViewModel
            {
                Id = section.Id,
                Name = section.Name
            })
            .ToList();

        if (!viewModel.SectionOptions.Any())
        {
            return viewModel;
        }

        var selectedSectionId = requestedSectionId.HasValue && viewModel.SectionOptions.Any(s => s.Id == requestedSectionId.Value)
            ? requestedSectionId.Value
            : viewModel.SectionOptions.First().Id;

        viewModel.SelectedSectionId = selectedSectionId;

        var selectedSection = sections.FirstOrDefault(s => s.Id == selectedSectionId);
        if (selectedSection is not null)
        {
            viewModel.SelectedSectionName = selectedSection.Name;
            viewModel.SelectedSectionSubjectId = selectedSection.SubjectId;
            viewModel.SelectedSectionSubjectName = string.IsNullOrWhiteSpace(selectedSection.SubjectName)
                ? "-"
                : selectedSection.SubjectName;
        }

        await PopulateTimetableSubjectOptionsAsync(viewModel, selectedSection);
        await PopulateTeacherAssignmentStateAsync(viewModel, currentUserId, role, selectedSectionId);

        var timetableResult = await TryCallAsync(() => _sectionsService.GetTimetableAsync(selectedSectionId, currentUserId));
        if (!timetableResult.Success || timetableResult.Data is null)
        {
            viewModel.TimetableErrorMessage = timetableResult.Error ?? "Unable to load timetable right now.";
            return viewModel;
        }

        var slots = new List<TimetableSlotRecord>();
        foreach (var dayEntry in timetableResult.Data.Timetable)
        {
            foreach (var slot in dayEntry.Value)
            {
                if (!TimeOnly.TryParse(slot.StartTime, out var parsedStart)
                    || !TimeOnly.TryParse(slot.EndTime, out var parsedEnd))
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
        return viewModel;
    }

    public async Task<SectionAttendanceIndexViewModel> BuildSectionAttendanceIndexViewModelAsync(
        int currentUserId,
        string role,
        int? requestedSectionId,
        int? requestedScheduleId,
        DateOnly? requestedAttendanceDate)
    {
        var viewModel = new SectionAttendanceIndexViewModel
        {
            IsAdmin = role.IsRole(UserRole.Admin),
            IsTeacher = role.IsRole(UserRole.Teacher),
            SelectedAttendanceDate = requestedAttendanceDate ?? DateOnly.FromDateTime(DateTime.Today)
        };

        var sectionsResult = await TryCallAsync(() => _sectionsService.GetAllSectionsAsync());
        if (!sectionsResult.Success || sectionsResult.Data is null)
        {
            viewModel.ErrorMessage = sectionsResult.Error ?? "Unable to load sections right now.";
            return viewModel;
        }

        viewModel.SectionOptions = sectionsResult.Data
            .OrderBy(section => section.Name)
            .Select(section => new SectionOptionViewModel
            {
                Id = section.Id,
                Name = section.Name
            })
            .ToList();

        if (!viewModel.SectionOptions.Any())
        {
            return viewModel;
        }

        var selectedSectionId = requestedSectionId.HasValue && viewModel.SectionOptions.Any(s => s.Id == requestedSectionId.Value)
            ? requestedSectionId.Value
            : viewModel.SectionOptions.First().Id;

        viewModel.SelectedSectionId = selectedSectionId;
        await PopulateAttendancePanelAsync(viewModel, currentUserId, role, selectedSectionId, requestedScheduleId);

        return viewModel;
    }

    public async Task<(bool Success, TeacherContext Context, string? Error)> BuildTeacherContextAsync(int userId, string role)
    {
        var isAdmin = role.IsRole(UserRole.Admin);
        if (isAdmin)
        {
            return (true, new TeacherContext { UserId = userId, TeacherId = null, IsAdmin = true }, null);
        }

        var teachersResult = await TryCallAsync(() => _teachersService.GetAllTeachersAsync());
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

    public async Task<(bool IsValid, string ErrorMessage)> ValidateSubjectSelectionForSectionAsync(int sectionId, int subjectId)
    {
        if (subjectId <= 0)
        {
            return (false, "Select a subject before adding a timetable slot.");
        }

        var sectionResult = await TryCallAsync(() => _sectionsService.GetSectionByIdAsync(sectionId));
        if (!sectionResult.Success || sectionResult.Data is null)
        {
            return (false, sectionResult.Error ?? "Unable to load the selected section.");
        }

        var subjectResult = await TryCallAsync(() => _subjectsService.GetSubjectByIdAsync(subjectId));
        if (!subjectResult.Success || subjectResult.Data is null)
        {
            return (false, subjectResult.Error ?? "Selected subject was not found.");
        }

        return (true, string.Empty);
    }

    private async Task PopulateAttendancePanelAsync(
        SectionAttendanceIndexViewModel viewModel,
        int currentUserId,
        string role,
        int selectedSectionId,
        int? requestedScheduleId)
    {
        var schedulesResult = await TryCallAsync(() => _schedulesService.GetSchedulesAsync(currentUserId, role));
        if (!schedulesResult.Success || schedulesResult.Data is null)
        {
            viewModel.AttendanceErrorMessage = schedulesResult.Error ?? "Unable to load schedules for attendance.";
            return;
        }

        var allowedSchedules = schedulesResult.Data
            .Where(schedule => schedule.SectionId == selectedSectionId);

        if (role.IsRole(UserRole.Teacher))
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

        var studentsResult = await TryCallAsync(() => _studentsService.GetStudentsBySectionAsync(selectedSectionId, currentUserId, role));
        if (!studentsResult.Success || studentsResult.Data is null)
        {
            viewModel.AttendanceErrorMessage = studentsResult.Error ?? "Unable to load students for the selected section.";
            return;
        }

        var summaryResult = await TryCallAsync(() => _attendanceService.GetSectionAttendanceAsync(
            selectedSectionId,
            viewModel.SelectedAttendanceDate,
            selectedScheduleId.Value,
            currentUserId,
            role));

        if (!summaryResult.Success || summaryResult.Data is null)
        {
            viewModel.AttendanceErrorMessage = summaryResult.Error ?? "Unable to load attendance summary.";
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
        var statusLabel = hasRecord ? record!.StatusLabel : AttendancePolicy.ToLabel(AttendanceStatusKind.Unmarked);
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

    private async Task PopulateCreateSectionOptionsAsync(SectionManagementIndexViewModel viewModel)
    {
        if (!viewModel.IsAdmin)
        {
            return;
        }

        // These lookups share a request-scoped DbContext under the hood.
        // Run them sequentially to avoid concurrent DbContext operations.
        var subjectsResult = await TryCallAsync(() => _subjectsService.GetAllSubjectsAsync());
        List<AcademicYearDto>? academicPeriods = null;
        List<CourseDto>? courses = null;
        List<ClassroomDto>? classrooms = null;

        var lookupLoadFailures = new List<string>();

        try
        {
            academicPeriods = await _academicYearsService.GetAllAcademicYearsAsync();
        }
        catch
        {
            lookupLoadFailures.Add("academic periods");
        }

        if (academicPeriods is not null)
        {
            viewModel.AcademicPeriods = academicPeriods
                .OrderByDescending(period => period.StartDate)
                .Select(period => new SectionReferenceOptionViewModel
                {
                    Id = period.Id,
                    Label = period.YearLabel
                })
                .ToList();
        }

        try
        {
            courses = await _coursesService.GetAllCoursesAsync();
        }
        catch
        {
            lookupLoadFailures.Add("courses");
        }

        if (courses is not null)
        {
            viewModel.Courses = courses
                .OrderBy(course => course.Name)
                .Select(course =>
                {
                    var yearRange = EducationLevelPolicy.GetAllowedYearRange(course.EducationLevel);
                    return new SectionReferenceOptionViewModel
                    {
                        Id = course.Id,
                        EducationLevel = course.EducationLevel,
                        EducationLevelLabel = EducationLevelPolicy.ToDisplayLabel(course.EducationLevel),
                        MinYearLevel = yearRange.MinYearLevel,
                        MaxYearLevel = yearRange.MaxYearLevel,
                        Label = string.IsNullOrWhiteSpace(course.Code)
                            ? course.Name
                            : $"{course.Code} - {course.Name}"
                    };
                })
                .ToList();

            if (viewModel.CreateForm.CourseId <= 0 && viewModel.Courses.Count > 0)
            {
                viewModel.CreateForm.CourseId = viewModel.Courses[0].Id;
            }

            var selectedCourse = viewModel.Courses.FirstOrDefault(course => course.Id == viewModel.CreateForm.CourseId);
            if (selectedCourse?.MinYearLevel is int minYearLevel
                && selectedCourse.MaxYearLevel is int maxYearLevel
                && (viewModel.CreateForm.YearLevel < minYearLevel || viewModel.CreateForm.YearLevel > maxYearLevel))
            {
                viewModel.CreateForm.YearLevel = minYearLevel;
            }
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

        try
        {
            classrooms = await _classroomsService.GetAllClassroomsAsync();
        }
        catch
        {
            lookupLoadFailures.Add("classrooms");
        }

        if (classrooms is not null)
        {
            viewModel.Classrooms = classrooms
                .OrderBy(classroom => classroom.Name)
                .Select(classroom => new SectionReferenceOptionViewModel
                {
                    Id = classroom.Id,
                    Label = classroom.Name
                })
                .ToList();
        }

        if (lookupLoadFailures.Count > 0)
        {
            viewModel.CreateSectionOptionsErrorMessage =
                $"Some create form options could not be loaded ({string.Join(", ", lookupLoadFailures)}).";
        }
    }

    private async Task PopulateTimetableSubjectOptionsAsync(TimetableIndexViewModel viewModel, SectionDto? selectedSection)
    {
        viewModel.TimetableSubjects = [];
        viewModel.TimetableSubjectsErrorMessage = null;

        if (selectedSection is null)
        {
            return;
        }

        var subjectsResult = await TryCallAsync(() => _subjectsService.GetAllSubjectsAsync());
        if (!subjectsResult.Success || subjectsResult.Data is null)
        {
            viewModel.TimetableSubjectsErrorMessage = subjectsResult.Error ?? "Unable to load timetable subject options right now.";
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

    private async Task PopulateTeacherAssignmentStateAsync(TimetableIndexViewModel viewModel, int currentUserId, string role, int selectedSectionId)
    {
        viewModel.IsCurrentTeacherAssignedToSelectedSection = false;

        if (!role.IsRole(UserRole.Teacher))
        {
            return;
        }

        var teachersResult = await TryCallAsync(() => _teachersService.GetAllTeachersAsync());
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

        var sectionTeachersResult = await TryCallAsync(() => _sectionsService.GetSectionTeachersAsync(selectedSectionId));
        if (!sectionTeachersResult.Success || sectionTeachersResult.Data is null)
        {
            return;
        }

        viewModel.IsCurrentTeacherAssignedToSelectedSection = sectionTeachersResult.Data
            .Any(assignment => assignment.TeacherId == teacherId.Value);
    }

    private async Task PopulateTeacherOptionsAsync(SectionManagementIndexViewModel viewModel)
    {
        if (!viewModel.IsAdmin)
        {
            return;
        }

        var teachersResult = await TryCallAsync(() => _teachersService.GetAllTeachersWithSectionsAsync());
        if (!teachersResult.Success || teachersResult.Data is null)
        {
            viewModel.TeacherOptionsErrorMessage = teachersResult.Error ?? "Unable to load teacher options right now.";
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

                if (occupiedSlot is null)
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

    private static async Task<(bool Success, T? Data, string? Error)> TryCallAsync<T>(Func<Task<T>> serviceCall)
    {
        try
        {
            var data = await serviceCall();
            return (true, data, null);
        }
        catch (Exception ex)
        {
            return (false, default, ex.Message);
        }
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
