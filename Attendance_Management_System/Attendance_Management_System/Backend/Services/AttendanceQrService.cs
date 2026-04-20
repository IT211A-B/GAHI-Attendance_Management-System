using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

public class AttendanceQrService : IAttendanceQrService
{
    private const int DefaultSuggestionTake = 8;
    private const int MaxSuggestionTake = 20;
    private const int MaxLiveFeedItems = 200;
    private const string UnknownStudentName = "Unknown student";

    private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private static readonly JsonSerializerOptions TokenSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _context;
    private readonly IAttendanceService _attendanceService;
    private readonly AttendanceSettings _attendanceSettings;
    private readonly AttendanceQrSettings _qrSettings;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AttendanceQrService> _logger;

    public AttendanceQrService(
        AppDbContext context,
        IAttendanceService attendanceService,
        IOptions<AttendanceSettings> attendanceSettings,
        IOptions<AttendanceQrSettings> qrSettings,
        INotificationService notificationService,
        ILogger<AttendanceQrService> logger)
    {
        _context = context;
        _attendanceService = attendanceService;
        _attendanceSettings = attendanceSettings.Value?.IsValid() == true
            ? attendanceSettings.Value
            : AttendanceSettings.Default;
        _qrSettings = qrSettings.Value?.IsValid() == true
            ? qrSettings.Value
            : AttendanceQrSettings.Default;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ApiResponse<List<AttendanceQrSectionSuggestionDto>>> SearchSectionsAsync(int userId, string role, string? query, int take)
    {
        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<List<AttendanceQrSectionSuggestionDto>>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var normalizedQuery = NormalizeQuery(query);
        var limit = ClampTake(take);

        var assignedSectionIdsQuery = _context.SectionTeachers
            .AsNoTracking()
            .Where(assignment => assignment.TeacherId == ownerContext.TeacherId)
            .Select(assignment => assignment.SectionId)
            .Union(_context.Schedules
                .AsNoTracking()
                .Where(schedule => schedule.TeacherId == ownerContext.TeacherId)
                .Select(schedule => schedule.SectionId));

        var sectionsQuery = _context.Sections
            .AsNoTracking()
            .Where(section => assignedSectionIdsQuery.Contains(section.Id));

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            sectionsQuery = sectionsQuery.Where(section =>
                section.Name.ToLower().Contains(normalizedQuery));
        }

        var sections = await sectionsQuery
            .Select(section => new AttendanceQrSectionSuggestionDto
            {
                SectionId = section.Id,
                SectionName = section.Name
            })
            .OrderBy(item => item.SectionName)
            .Take(limit)
            .ToListAsync();

        return ApiResponse<List<AttendanceQrSectionSuggestionDto>>.SuccessResponse(sections);
    }

    public async Task<ApiResponse<List<AttendanceQrSubjectSuggestionDto>>> SearchSubjectsAsync(int userId, string role, int sectionId, string? query, int take)
    {
        if (sectionId <= 0)
        {
            return ApiResponse<List<AttendanceQrSubjectSuggestionDto>>.ErrorResponse(
                ErrorCodes.ValidationError,
                "Section is required.");
        }

        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<List<AttendanceQrSubjectSuggestionDto>>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var normalizedQuery = NormalizeQuery(query);
        var limit = ClampTake(take);

        var schedulesQuery = BuildOwnedSchedulesForSchoolDateQuery(ownerContext.TeacherId, ownerContext.SchoolDate)
            .Where(schedule => schedule.SectionId == sectionId && schedule.Subject != null);

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            schedulesQuery = schedulesQuery.Where(schedule =>
                schedule.Subject != null
                && ((schedule.Subject.Name ?? string.Empty).ToLower().Contains(normalizedQuery)
                    || (schedule.Subject.Code ?? string.Empty).ToLower().Contains(normalizedQuery)));
        }

        var subjects = await schedulesQuery
            .Select(schedule => new
            {
                schedule.SubjectId,
                SubjectName = schedule.Subject!.Name,
                SubjectCode = schedule.Subject.Code
            })
            .Distinct()
            .OrderBy(item => item.SubjectName)
            .Take(limit)
            .ToListAsync();

        var result = subjects
            .Select(item => new AttendanceQrSubjectSuggestionDto
            {
                SubjectId = item.SubjectId,
                SubjectName = item.SubjectName,
                SubjectCode = item.SubjectCode,
                Label = BuildSubjectLabel(item.SubjectCode, item.SubjectName)
            })
            .ToList();

        return ApiResponse<List<AttendanceQrSubjectSuggestionDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<List<AttendanceQrPeriodSuggestionDto>>> SearchPeriodsAsync(int userId, string role, int sectionId, int subjectId, string? query, int take)
    {
        if (sectionId <= 0 || subjectId <= 0)
        {
            return ApiResponse<List<AttendanceQrPeriodSuggestionDto>>.ErrorResponse(
                ErrorCodes.ValidationError,
                "Section and subject are required.");
        }

        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<List<AttendanceQrPeriodSuggestionDto>>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var normalizedQuery = NormalizeQuery(query);
        var limit = ClampTake(take);

        var periodRows = await BuildOwnedSchedulesForSchoolDateQuery(ownerContext.TeacherId, ownerContext.SchoolDate)
            .Where(schedule => schedule.SectionId == sectionId && schedule.SubjectId == subjectId)
            .OrderBy(schedule => schedule.StartTime)
            .ThenBy(schedule => schedule.EndTime)
            .Select(schedule => new
            {
                schedule.Id,
                schedule.SectionId,
                schedule.SubjectId,
                schedule.DayOfWeek,
                schedule.StartTime,
                schedule.EndTime
            })
            .ToListAsync();

        var periods = periodRows
            .Select(row =>
            {
                var dayName = ResolveDayName(row.DayOfWeek);
                var timeRange = $"{row.StartTime:HH\\:mm}-{row.EndTime:HH\\:mm}";
                return new AttendanceQrPeriodSuggestionDto
                {
                    ScheduleId = row.Id,
                    SectionId = row.SectionId,
                    SubjectId = row.SubjectId,
                    DayName = dayName,
                    StartTime = row.StartTime.ToString("HH:mm"),
                    EndTime = row.EndTime.ToString("HH:mm"),
                    TimeRangeLabel = timeRange,
                    Label = $"{dayName} | {timeRange}"
                };
            })
            .Where(period => string.IsNullOrWhiteSpace(normalizedQuery)
                || period.Label.ToLower().Contains(normalizedQuery)
                || period.TimeRangeLabel.ToLower().Contains(normalizedQuery))
            .Take(limit)
            .ToList();

        return ApiResponse<List<AttendanceQrPeriodSuggestionDto>>.SuccessResponse(periods);
    }

    public async Task<ApiResponse<AttendanceQrSessionDto>> CreateSessionAsync(int userId, string role, CreateAttendanceQrSessionRequest request)
    {
        if (request.SectionId <= 0 || request.SubjectId <= 0 || request.ScheduleId <= 0)
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                ErrorCodes.ValidationError,
                "Section, subject, and period are required.");
        }

        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var schoolDate = ownerContext.SchoolDate;
        var nowUtc = DateTimeOffset.UtcNow;

        var schedule = await _context.Schedules
            .Include(item => item.Section)
            .Include(item => item.Subject)
            .FirstOrDefaultAsync(item =>
                item.Id == request.ScheduleId
                && item.SectionId == request.SectionId
                && item.SubjectId == request.SubjectId
                && item.TeacherId == ownerContext.TeacherId);

        if (schedule == null)
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                ErrorCodes.Forbidden,
                "Selected period is not owned by your account.");
        }

        if (!AttendancePolicy.IsDateAlignedWithSchedule(schedule, schoolDate))
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                "OUTSIDE_ALLOWED_WINDOW",
                "Selected period is not active for today.");
        }

        // Close any still-active session for the same owner+schedule before creating a fresh one.
        var activeSessions = await _context.AttendanceQrSessions
            .Where(session => session.OwnerTeacherId == ownerContext.TeacherId
                && session.ScheduleId == schedule.Id
                && session.IsActive
                && session.ExpiresAtUtc > nowUtc)
            .ToListAsync();

        foreach (var activeSession in activeSessions)
        {
            activeSession.IsActive = false;
            activeSession.ClosedAtUtc = nowUtc;
        }

        var sessionId = $"qrs_{Guid.NewGuid():N}";
        var nonce = Guid.NewGuid().ToString("N");
        var issuedAtUtc = nowUtc;
        var expiresAtUtc = issuedAtUtc.AddSeconds(_qrSettings.SessionTtlSeconds);

        var session = new AttendanceQrSession
        {
            SessionId = sessionId,
            SectionId = schedule.SectionId,
            SubjectId = schedule.SubjectId,
            ScheduleId = schedule.Id,
            CreatedByUserId = userId,
            OwnerTeacherId = ownerContext.TeacherId,
            IssuedAtUtc = issuedAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            IsActive = true,
            TokenNonce = nonce,
            ClosedAtUtc = null
        };

        _context.AttendanceQrSessions.Add(session);
        await _context.SaveChangesAsync();

        var token = BuildSignedToken(session);
        var dto = BuildSessionDto(
            session,
            token,
            sectionName: schedule.Section?.Name ?? "-",
            subjectLabel: BuildSubjectLabel(schedule.Subject?.Code, schedule.Subject?.Name),
            periodLabel: BuildPeriodLabel(schedule.DayOfWeek, schedule.StartTime, schedule.EndTime),
            timeRangeLabel: $"{schedule.StartTime:HH\\:mm}-{schedule.EndTime:HH\\:mm}");

        return ApiResponse<AttendanceQrSessionDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<AttendanceQrSessionDto>> RefreshSessionAsync(int userId, string role, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                ErrorCodes.ValidationError,
                "Session id is required.");
        }

        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var session = await _context.AttendanceQrSessions
            .Include(item => item.Schedule)
            .Include(item => item.Section)
            .Include(item => item.Subject)
            .FirstOrDefaultAsync(item => item.SessionId == sessionId);

        if (session == null)
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse("SESSION_INACTIVE", "QR session not found.");
        }

        if (session.OwnerTeacherId != ownerContext.TeacherId)
        {
            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(ErrorCodes.Forbidden, "You do not own this QR session.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        if (!session.IsActive || nowUtc >= session.ExpiresAtUtc)
        {
            session.IsActive = false;
            session.ClosedAtUtc ??= nowUtc;
            await _context.SaveChangesAsync();

            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                "SESSION_INACTIVE",
                "QR session is no longer active.");
        }

        var schedule = session.Schedule;
        if (schedule == null)
        {
            schedule = await _context.Schedules.FirstOrDefaultAsync(item => item.Id == session.ScheduleId);
        }

        if (schedule == null)
        {
            session.IsActive = false;
            session.ClosedAtUtc = nowUtc;
            await _context.SaveChangesAsync();

            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                ErrorCodes.NotFound,
                "Associated schedule not found.");
        }

        if (!AttendancePolicy.IsDateAlignedWithSchedule(schedule, ownerContext.SchoolDate))
        {
            session.IsActive = false;
            session.ClosedAtUtc = nowUtc;
            await _context.SaveChangesAsync();

            return ApiResponse<AttendanceQrSessionDto>.ErrorResponse(
                "OUTSIDE_ALLOWED_WINDOW",
                "Schedule is no longer active for today.");
        }

        session.IssuedAtUtc = nowUtc;
        session.ExpiresAtUtc = nowUtc.AddSeconds(_qrSettings.SessionTtlSeconds);
        session.TokenNonce = Guid.NewGuid().ToString("N");

        await _context.SaveChangesAsync();

        var token = BuildSignedToken(session);
        var dto = BuildSessionDto(
            session,
            token,
            sectionName: session.Section?.Name ?? "-",
            subjectLabel: BuildSubjectLabel(session.Subject?.Code, session.Subject?.Name),
            periodLabel: BuildPeriodLabel(schedule.DayOfWeek, schedule.StartTime, schedule.EndTime),
            timeRangeLabel: $"{schedule.StartTime:HH\\:mm}-{schedule.EndTime:HH\\:mm}");

        return ApiResponse<AttendanceQrSessionDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<bool>> CloseSessionAsync(int userId, string role, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return ApiResponse<bool>.ErrorResponse(
                ErrorCodes.ValidationError,
                "Session id is required.");
        }

        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<bool>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var session = await _context.AttendanceQrSessions
            .FirstOrDefaultAsync(item => item.SessionId == sessionId);

        if (session == null)
        {
            return ApiResponse<bool>.ErrorResponse("SESSION_INACTIVE", "QR session not found.");
        }

        if (session.OwnerTeacherId != ownerContext.TeacherId)
        {
            return ApiResponse<bool>.ErrorResponse(ErrorCodes.Forbidden, "You do not own this QR session.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var hasChanges = false;

        if (session.IsActive)
        {
            session.IsActive = false;
            hasChanges = true;
        }

        if (!session.ClosedAtUtc.HasValue)
        {
            session.ClosedAtUtc = nowUtc;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }

        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<AttendanceQrLiveFeedDto>> GetLiveFeedAsync(int userId, string role, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return ApiResponse<AttendanceQrLiveFeedDto>.ErrorResponse(
                ErrorCodes.ValidationError,
                "Session id is required.");
        }

        var ownerContext = await ResolveOwnerContextAsync(userId, role);
        if (!ownerContext.Success)
        {
            return ApiResponse<AttendanceQrLiveFeedDto>.ErrorResponse(ownerContext.ErrorCode, ownerContext.ErrorMessage);
        }

        var session = await _context.AttendanceQrSessions
            .AsNoTracking()
            .Include(item => item.Schedule)
            .Include(item => item.Section)
            .Include(item => item.Subject)
            .FirstOrDefaultAsync(item => item.SessionId == sessionId);

        if (session == null)
        {
            return ApiResponse<AttendanceQrLiveFeedDto>.ErrorResponse("SESSION_INACTIVE", "QR session not found.");
        }

        if (session.OwnerTeacherId != ownerContext.TeacherId)
        {
            return ApiResponse<AttendanceQrLiveFeedDto>.ErrorResponse(ErrorCodes.Forbidden, "You do not own this QR session.");
        }

        var checkinRows = await _context.AttendanceQrCheckins
            .AsNoTracking()
            .Where(item => item.AttendanceQrSessionId == session.Id)
            .OrderByDescending(item => item.CheckedInAtUtc)
            .Take(MaxLiveFeedItems)
            .Select(item => new LiveFeedCheckinRow
            {
                StudentId = item.StudentId,
                StudentNumber = item.Student != null ? item.Student.StudentNumber : string.Empty,
                FirstName = item.Student != null ? item.Student.FirstName : null,
                MiddleName = item.Student != null ? item.Student.MiddleName : null,
                LastName = item.Student != null ? item.Student.LastName : null,
                CheckedInAtUtc = item.CheckedInAtUtc,
                Status = item.Status
            })
            .ToListAsync();

        var checkins = checkinRows
            .Select(item => new AttendanceQrCheckinFeedItemDto
            {
                StudentId = item.StudentId,
                StudentNumber = item.StudentNumber,
                StudentName = BuildStudentName(item.FirstName, item.MiddleName, item.LastName),
                CheckedInAtUtc = item.CheckedInAtUtc,
                Status = item.Status
            })
            .ToList();

        var schedule = session.Schedule;
        var periodLabel = schedule != null
            ? BuildPeriodLabel(schedule.DayOfWeek, schedule.StartTime, schedule.EndTime)
            : "-";

        var dto = new AttendanceQrLiveFeedDto
        {
            SessionId = session.SessionId,
            SectionName = session.Section?.Name ?? "-",
            SubjectLabel = BuildSubjectLabel(session.Subject?.Code, session.Subject?.Name),
            PeriodLabel = periodLabel,
            ExpiresAtUtc = session.ExpiresAtUtc,
            IsActive = session.IsActive && session.ExpiresAtUtc > DateTimeOffset.UtcNow,
            Checkins = checkins
        };

        return ApiResponse<AttendanceQrLiveFeedDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<AttendanceQrCheckinResultDto>> SubmitCheckinAsync(int userId, string role, SubmitAttendanceQrCheckinRequest request)
    {
        if (!string.Equals(role, "student", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                ErrorCodes.Forbidden,
                "Only students can submit QR check-ins.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse("TOKEN_INVALID", "QR token is required.");
        }

        if (!TryParseAndValidateToken(request.Token.Trim(), out var payload, out var tokenErrorCode, out var tokenErrorMessage))
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(tokenErrorCode, tokenErrorMessage);
        }

        var session = await _context.AttendanceQrSessions
            .Include(item => item.Schedule)
            .FirstOrDefaultAsync(item => item.SessionId == payload.SessionId);

        if (session == null)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse("SESSION_INACTIVE", "QR session not found.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        if (!session.IsActive || nowUtc >= session.ExpiresAtUtc)
        {
            session.IsActive = false;
            session.ClosedAtUtc ??= nowUtc;
            await _context.SaveChangesAsync();

            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                "SESSION_INACTIVE",
                "QR session is no longer active.");
        }

        if (!TokenPayloadMatchesSession(payload, session))
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                "TOKEN_INVALID",
                "QR token does not match the active session.");
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.IsActive);

        if (student == null)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                ErrorCodes.Forbidden,
                "Student profile not found for this account.");
        }

        var activeAcademicYearId = await _context.AcademicYears
            .AsNoTracking()
            .Where(year => year.IsActive)
            .Select(year => (int?)year.Id)
            .FirstOrDefaultAsync();

        if (!activeAcademicYearId.HasValue)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                ErrorCodes.NotFound,
                "No active academic year found.");
        }

        var hasApprovedEnrollment = await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(enrollment =>
                enrollment.StudentId == student.Id
                && enrollment.SectionId == session.SectionId
                && enrollment.AcademicYearId == activeAcademicYearId.Value
                && enrollment.Status == "approved");

        if (!hasApprovedEnrollment || student.SectionId != session.SectionId)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                "STUDENT_NOT_ENROLLED",
                "You are not enrolled in this class section.");
        }

        var alreadyCheckedIn = await _context.AttendanceQrCheckins
            .AsNoTracking()
            .AnyAsync(item => item.AttendanceQrSessionId == session.Id && item.StudentId == student.Id);

        if (alreadyCheckedIn)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                "ALREADY_CHECKED_IN",
                "You are already marked present for this session.");
        }

        var schoolNow = TimeZoneInfo.ConvertTime(nowUtc, AttendancePolicy.ResolveSchoolTimeZone(_attendanceSettings));
        var attendanceRequest = new MarkAttendanceRequest
        {
            SectionId = session.SectionId,
            ScheduleId = session.ScheduleId,
            StudentId = student.Id,
            Date = DateOnly.FromDateTime(schoolNow.DateTime),
            TimeIn = TimeOnly.FromDateTime(schoolNow.DateTime),
            Remarks = "QR check-in"
        };

        var teacherContext = new TeacherContext
        {
            UserId = session.CreatedByUserId,
            TeacherId = session.OwnerTeacherId,
            IsAdmin = false
        };

        var attendanceResult = await _attendanceService.MarkAttendanceAsync(attendanceRequest, teacherContext);
        if (!attendanceResult.Success || attendanceResult.Data is null)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                attendanceResult.Error?.Code ?? ErrorCodes.BadRequest,
                attendanceResult.Error?.Message ?? "Unable to submit attendance.");
        }

        var normalizedStatus = NormalizeStatus(attendanceResult.Data.StatusLabel);
        var studentName = BuildStudentName(student.FirstName, student.MiddleName, student.LastName);

        var checkin = new AttendanceQrCheckin
        {
            AttendanceQrSessionId = session.Id,
            StudentId = student.Id,
            CheckedInAtUtc = nowUtc,
            Status = normalizedStatus,
            AttendanceId = attendanceResult.Data.Id > 0 ? attendanceResult.Data.Id : null
        };

        _context.AttendanceQrCheckins.Add(checkin);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ApiResponse<AttendanceQrCheckinResultDto>.ErrorResponse(
                "ALREADY_CHECKED_IN",
                "You are already marked present for this session.");
        }

        await TryCreateCheckinNotificationAsync(session, student.Id, studentName, normalizedStatus);

        var result = new AttendanceQrCheckinResultDto
        {
            SessionId = session.SessionId,
            Status = normalizedStatus,
            Message = normalizedStatus == "late"
                ? "Attendance submitted. You are marked late."
                : "Attendance submitted successfully.",
            RecordedAtUtc = checkin.CheckedInAtUtc
        };

        return ApiResponse<AttendanceQrCheckinResultDto>.SuccessResponse(result);
    }

    private async Task<(bool Success, int TeacherId, DateOnly SchoolDate, string ErrorCode, string ErrorMessage)> ResolveOwnerContextAsync(int userId, string role)
    {
        var isTeacherRole = string.Equals(role, "teacher", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);

        if (!isTeacherRole)
        {
            return (false, 0, default, ErrorCodes.Forbidden, "Only teacher accounts can use QR teacher controls.");
        }

        var teacherId = await _context.Teachers
            .AsNoTracking()
            .Where(teacher => teacher.UserId == userId)
            .Select(teacher => (int?)teacher.Id)
            .FirstOrDefaultAsync();

        if (!teacherId.HasValue)
        {
            return (false, 0, default, ErrorCodes.Forbidden, "QR attendance requires a teacher profile assigned to this account.");
        }

        var schoolDate = AttendancePolicy.GetSchoolDate(_attendanceSettings, DateTimeOffset.UtcNow);
        return (true, teacherId.Value, schoolDate, string.Empty, string.Empty);
    }

    private IQueryable<Schedule> BuildOwnedSchedulesForSchoolDateQuery(int teacherId, DateOnly schoolDate)
    {
        return _context.Schedules
            .AsNoTracking()
            .Where(schedule => schedule.TeacherId == teacherId
                && schedule.DayOfWeek == (int)schoolDate.DayOfWeek
                && schoolDate >= schedule.EffectiveFrom
                && (!schedule.EffectiveTo.HasValue || schoolDate <= schedule.EffectiveTo.Value));
    }

    private static int ClampTake(int take)
    {
        if (take <= 0)
        {
            return DefaultSuggestionTake;
        }

        return Math.Min(take, MaxSuggestionTake);
    }

    private static string NormalizeQuery(string? query)
    {
        return string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : query.Trim().ToLowerInvariant();
    }

    private static string ResolveDayName(int dayOfWeek)
    {
        return dayOfWeek >= 0 && dayOfWeek < DayNames.Length
            ? DayNames[dayOfWeek]
            : "Unknown";
    }

    private static string BuildSubjectLabel(string? subjectCode, string? subjectName)
    {
        var cleanName = string.IsNullOrWhiteSpace(subjectName) ? "Unknown subject" : subjectName.Trim();
        if (string.IsNullOrWhiteSpace(subjectCode))
        {
            return cleanName;
        }

        return $"{subjectCode.Trim()} - {cleanName}";
    }

    private static string BuildStudentName(string? firstName, string? middleName, string? lastName)
    {
        var fullName = string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim()));

        return string.IsNullOrWhiteSpace(fullName) ? UnknownStudentName : fullName;
    }

    private static string BuildPeriodLabel(int dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        return $"{ResolveDayName(dayOfWeek)} | {startTime:HH\\:mm}-{endTime:HH\\:mm}";
    }

    private async Task TryCreateCheckinNotificationAsync(
        AttendanceQrSession session,
        int studentId,
        string studentName,
        string normalizedStatus)
    {
        try
        {
            var payloadJson = JsonSerializer.Serialize(new
            {
                SessionId = session.SessionId,
                StudentId = studentId,
                StudentName = studentName,
                Status = normalizedStatus
            });

            await _notificationService.CreateAsync(
                session.CreatedByUserId,
                NotificationTypes.Checkin,
                "QR Check-in",
                $"{studentName} checked in via QR.",
                NotificationLinks.AttendanceQr,
                payloadJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create QR check-in notification for session {SessionId}.", session.SessionId);
        }
    }

    private AttendanceQrSessionDto BuildSessionDto(
        AttendanceQrSession session,
        string token,
        string sectionName,
        string subjectLabel,
        string periodLabel,
        string timeRangeLabel)
    {
        return new AttendanceQrSessionDto
        {
            SessionId = session.SessionId,
            Token = token,
            ExpiresAtUtc = session.ExpiresAtUtc,
            RefreshAfterSeconds = _qrSettings.RefreshThresholdSeconds,
            SectionName = sectionName,
            SubjectLabel = subjectLabel,
            PeriodLabel = periodLabel,
            TimeRangeLabel = timeRangeLabel
        };
    }

    private string BuildSignedToken(AttendanceQrSession session)
    {
        var payload = new AttendanceQrTokenPayload
        {
            Version = 1,
            SessionId = session.SessionId,
            SectionId = session.SectionId,
            SubjectId = session.SubjectId,
            ScheduleId = session.ScheduleId,
            ExpiresAtUnixMs = session.ExpiresAtUtc.ToUnixTimeMilliseconds(),
            Nonce = session.TokenNonce
        };

        var payloadJson = JsonSerializer.Serialize(payload, TokenSerializerOptions);
        var payloadEncoded = EncodeBase64Url(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeSignature(payloadEncoded);

        return $"{payloadEncoded}.{EncodeBase64Url(signature)}";
    }

    private bool TryParseAndValidateToken(
        string token,
        out AttendanceQrTokenPayload payload,
        out string errorCode,
        out string errorMessage)
    {
        payload = new AttendanceQrTokenPayload();
        errorCode = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
        {
            errorCode = "TOKEN_INVALID";
            errorMessage = "QR token format is invalid.";
            return false;
        }

        if (token.StartsWith("qrs_", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = "TOKEN_INVALID";
            errorMessage = "That value is a session ID, not a QR token. Scan the QR image or copy the full token from the QR Token panel.";
            return false;
        }

        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            errorCode = "TOKEN_INVALID";
            errorMessage = "QR token format is invalid.";
            return false;
        }

        var payloadEncoded = parts[0];
        var signatureEncoded = parts[1];

        byte[] providedSignature;
        try
        {
            providedSignature = DecodeBase64Url(signatureEncoded);
        }
        catch (FormatException)
        {
            errorCode = "TOKEN_INVALID";
            errorMessage = "QR token signature is invalid.";
            return false;
        }

        var expectedSignature = ComputeSignature(payloadEncoded);
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
        {
            errorCode = "TOKEN_INVALID";
            errorMessage = "QR token signature check failed.";
            return false;
        }

        try
        {
            var payloadBytes = DecodeBase64Url(payloadEncoded);
            var parsedPayload = JsonSerializer.Deserialize<AttendanceQrTokenPayload>(payloadBytes, TokenSerializerOptions);

            if (parsedPayload == null
                || string.IsNullOrWhiteSpace(parsedPayload.SessionId)
                || string.IsNullOrWhiteSpace(parsedPayload.Nonce)
                || parsedPayload.ExpiresAtUnixMs <= 0)
            {
                errorCode = "TOKEN_INVALID";
                errorMessage = "QR token payload is invalid.";
                return false;
            }

            var expiresAtUtc = DateTimeOffset.FromUnixTimeMilliseconds(parsedPayload.ExpiresAtUnixMs);
            if (DateTimeOffset.UtcNow >= expiresAtUtc)
            {
                errorCode = "TOKEN_EXPIRED";
                errorMessage = "QR token already expired.";
                return false;
            }

            payload = parsedPayload;
            return true;
        }
        catch (Exception)
        {
            errorCode = "TOKEN_INVALID";
            errorMessage = "QR token payload could not be parsed.";
            return false;
        }
    }

    private bool TokenPayloadMatchesSession(AttendanceQrTokenPayload payload, AttendanceQrSession session)
    {
        if (!string.Equals(payload.SessionId, session.SessionId, StringComparison.Ordinal))
        {
            return false;
        }

        if (payload.SectionId != session.SectionId
            || payload.SubjectId != session.SubjectId
            || payload.ScheduleId != session.ScheduleId)
        {
            return false;
        }

        if (!string.Equals(payload.Nonce, session.TokenNonce, StringComparison.Ordinal))
        {
            return false;
        }

        var sessionExpiryMs = session.ExpiresAtUtc.ToUnixTimeMilliseconds();
        return payload.ExpiresAtUnixMs == sessionExpiryMs;
    }

    private byte[] ComputeSignature(string payloadEncoded)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_qrSettings.SigningKey.Trim());
        var payloadBytes = Encoding.UTF8.GetBytes(payloadEncoded);

        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(payloadBytes);
    }

    private static string EncodeBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(normalized);
    }

    private static string NormalizeStatus(string? statusLabel)
    {
        return string.Equals(statusLabel, "Late", StringComparison.OrdinalIgnoreCase)
            ? "late"
            : "present";
    }

    private sealed class AttendanceQrTokenPayload
    {
        public int Version { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int SectionId { get; set; }
        public int SubjectId { get; set; }
        public int ScheduleId { get; set; }
        public long ExpiresAtUnixMs { get; set; }
        public string Nonce { get; set; } = string.Empty;
    }

    private sealed class LiveFeedCheckinRow
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public DateTimeOffset CheckedInAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
