namespace Attendance_Management_System.Backend.ViewModels.Attendance;

public class AttendanceQrPageViewModel
{
    public int LiveFeedPollSeconds { get; set; } = 3;
    public int RefreshThresholdSeconds { get; set; } = 60;
}
