namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAccountEmailService
{
    Task SendSignupAcknowledgmentAsync(string toAddress, string studentName);

    Task SendVerificationEmailAsync(string toAddress, string studentName, string confirmationLink);
}
