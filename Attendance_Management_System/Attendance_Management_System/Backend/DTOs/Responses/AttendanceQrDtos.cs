namespace Attendance_Management_System.Backend.DTOs.Responses;

public class AttendanceQrSectionSuggestionDto
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
}

public class AttendanceQrSubjectSuggestionDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class AttendanceQrPeriodSuggestionDto
{
    public int ScheduleId { get; set; }
    public int SectionId { get; set; }
    public int SubjectId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string TimeRangeLabel { get; set; } = string.Empty;
}

public class AttendanceQrSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public int RefreshAfterSeconds { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string SubjectLabel { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public string TimeRangeLabel { get; set; } = string.Empty;
}

public class AttendanceQrCheckinFeedItemDto
{
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public DateTimeOffset CheckedInAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AttendanceQrLiveFeedDto
{
    public string SessionId { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string SubjectLabel { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<AttendanceQrCheckinFeedItemDto> Checkins { get; set; } = [];
}

public class AttendanceQrCheckinResultDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
}
