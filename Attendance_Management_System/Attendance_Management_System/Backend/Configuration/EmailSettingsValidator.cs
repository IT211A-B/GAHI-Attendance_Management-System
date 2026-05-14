using System.Net.Mail;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Configuration;

public class EmailSettingsValidator : IValidateOptions<EmailSettings>
{
    private readonly bool _requireSmtpCredentials;

    public EmailSettingsValidator()
        : this(requireSmtpCredentials: false)
    {
    }

    public EmailSettingsValidator(IHostEnvironment hostEnvironment)
        : this(hostEnvironment?.IsProduction() == true)
    {
    }

    internal EmailSettingsValidator(bool requireSmtpCredentials)
    {
        _requireSmtpCredentials = requireSmtpCredentials;
    }

    public ValidateOptionsResult Validate(string? name, EmailSettings options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Email settings are required.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return ValidateOptionsResult.Fail($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} is required.");
        }

        var publicBaseUrl = options.PublicBaseUrl.Trim();

        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var parsedUri))
        {
            return ValidateOptionsResult.Fail($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} must be a valid absolute URL.");
        }

        if (!string.Equals(parsedUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} must use http or https.");
        }

        if (options.Port <= 0 || options.Port > 65535)
        {
            return ValidateOptionsResult.Fail($"{EmailSettings.SectionName}:{nameof(EmailSettings.Port)} must be between 1 and 65535.");
        }

        var usernameProvided = !string.IsNullOrWhiteSpace(options.Username);
        var passwordProvided = !string.IsNullOrWhiteSpace(options.Password);
        if (usernameProvided ^ passwordProvided)
        {
            return ValidateOptionsResult.Fail(
                $"{EmailSettings.SectionName}:{nameof(EmailSettings.Username)} and {EmailSettings.SectionName}:{nameof(EmailSettings.Password)} must both be provided when SMTP authentication is enabled.");
        }

        if (_requireSmtpCredentials && (!usernameProvided || !passwordProvided))
        {
            return ValidateOptionsResult.Fail(
                $"{EmailSettings.SectionName}:{nameof(EmailSettings.Username)} and {EmailSettings.SectionName}:{nameof(EmailSettings.Password)} are required in Production.");
        }

        if (!string.IsNullOrWhiteSpace(options.FromAddress)
            && !MailAddress.TryCreate(options.FromAddress.Trim(), out _))
        {
            return ValidateOptionsResult.Fail($"{EmailSettings.SectionName}:{nameof(EmailSettings.FromAddress)} must be a valid email address.");
        }

        return ValidateOptionsResult.Success;
    }
}
