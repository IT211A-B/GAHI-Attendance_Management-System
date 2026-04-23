using System.Net;
using Attendance_Management_System.Backend.Interfaces.Services;

namespace Attendance_Management_System.Backend.Services;

public class AccountEmailService : IAccountEmailService
{
    private readonly IEmailSender _emailSender;

    public AccountEmailService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendSignupAcknowledgmentAsync(string toAddress, string studentName)
    {
        var safeName = WebUtility.HtmlEncode(studentName);

        var subject = "Welcome to DonBosco AMS";
        var htmlBody = $"""
            <div style=\"font-family:Arial,sans-serif;line-height:1.6;color:#1f2937;\">
              <h2 style=\"margin-bottom:8px;\">Welcome, {safeName}!</h2>
              <p>Your DonBosco AMS signup was received successfully.</p>
              <p>Your enrollment request is now pending admin review. You will be notified once your status changes.</p>
              <p style=\"margin-top:20px;\">DonBosco AMS Team</p>
            </div>
            """;

        return _emailSender.SendAsync(toAddress, subject, htmlBody);
    }

    public Task SendVerificationEmailAsync(string toAddress, string studentName, string confirmationLink)
    {
        var safeName = WebUtility.HtmlEncode(studentName);
        var safeLink = WebUtility.HtmlEncode(confirmationLink);

        var subject = "Verify your DonBosco AMS email";
        var htmlBody = $"""
            <div style=\"font-family:Arial,sans-serif;line-height:1.6;color:#1f2937;\">
              <h2 style=\"margin-bottom:8px;\">Confirm your email address</h2>
              <p>Hello {safeName},</p>
              <p>Please confirm your email to activate your account and sign in.</p>
              <p>
                <a href=\"{safeLink}\" style=\"display:inline-block;padding:10px 16px;background:#0f766e;color:#ffffff;text-decoration:none;border-radius:6px;\">
                  Verify Email
                </a>
              </p>
              <p>If the button does not work, copy and paste this link into your browser:</p>
              <p>{safeLink}</p>
              <p style=\"margin-top:20px;\">DonBosco AMS Team</p>
            </div>
            """;

        return _emailSender.SendAsync(toAddress, subject, htmlBody);
    }
}
