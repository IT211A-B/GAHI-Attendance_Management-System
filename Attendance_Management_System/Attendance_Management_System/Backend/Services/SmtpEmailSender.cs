using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Interfaces.Services;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _emailSettings;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> emailSettings,
        IHostEnvironment hostEnvironment,
        ILogger<SmtpEmailSender> logger)
    {
        _emailSettings = emailSettings.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(toAddress));
        }

        if (!IsSmtpConfigured())
        {
            if (_hostEnvironment.IsDevelopment())
            {
                LogEmail(toAddress, subject, htmlBody);
                return;
            }

            throw new InvalidOperationException("EmailSettings are not fully configured for SMTP delivery.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = htmlBody
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, _emailSettings.UseSsl);

        if (!string.IsNullOrWhiteSpace(_emailSettings.Username) && !string.IsNullOrWhiteSpace(_emailSettings.Password))
        {
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private bool IsSmtpConfigured()
    {
        if (string.IsNullOrWhiteSpace(_emailSettings.Host)
            || string.Equals(_emailSettings.Host.Trim(), "smtp.example.com", StringComparison.OrdinalIgnoreCase)
            || _emailSettings.Port <= 0)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(_emailSettings.FromAddress);
    }

    private void LogEmail(string toAddress, string subject, string htmlBody)
    {
        _logger.LogInformation(
            "DEV EMAIL FALLBACK\nTo: {To}\nSubject: {Subject}\nBody:\n{Body}",
            toAddress,
            subject,
            htmlBody);
    }
}
