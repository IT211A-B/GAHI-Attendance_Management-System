namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IEmailSender
{
    Task SendAsync(string toAddress, string subject, string htmlBody);
}
