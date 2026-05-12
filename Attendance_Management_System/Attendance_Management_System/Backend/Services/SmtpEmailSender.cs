using Attendance_Management_System.Backend.Interfaces.Services;
using FluentEmail.Core;
 
namespace Attendance_Management_System.Backend.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IFluentEmailFactory _fluentEmailFactory;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IFluentEmailFactory fluentEmailFactory,
        ILogger<SmtpEmailSender> logger)
    {
        _fluentEmailFactory = fluentEmailFactory;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody)
    {
        EnsureRecipientAddress(toAddress);

        var sendResponse = await _fluentEmailFactory
            .Create()
            .To(toAddress)
            .Subject(subject ?? string.Empty)
            .Body(htmlBody ?? string.Empty, isHtml: true)
            .SendAsync();

        if (sendResponse.Successful)
        {
            return;
        }

        var deliveryErrors = sendResponse.ErrorMessages is { Count: > 0 }
            ? string.Join("; ", sendResponse.ErrorMessages)
            : "Unknown email delivery failure.";

        _logger.LogError(
            "Email delivery failed for {Recipient}. Errors: {Errors}",
            toAddress,
            deliveryErrors);

        throw new InvalidOperationException($"Email delivery failed for '{toAddress}'.");
    }

    private static void EnsureRecipientAddress(string toAddress)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            throw new ArgumentException("Recipient email address is required.", nameof(toAddress));
        }
    }
}
