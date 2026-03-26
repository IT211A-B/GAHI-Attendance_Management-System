namespace Attendance_Management_System.Backend.DTOs.Responses;

public class AttendanceDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public string? SubjectName { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? TimeIn { get; set; }
    public TimeOnly? TimeOut { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset MarkedAt { get; set; }
    public int MarkedBy { get; set; }
    public string? MarkerName { get; set; }
}