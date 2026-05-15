using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Configuration;

public class EmailSettingsValidator : IValidateOptions<EmailSettings>
{
    private readonly IHostEnvironment? _environment;

    public EmailSettingsValidator()
    {
    }

    public EmailSettingsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, EmailSettings options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Email settings are required.");
        }

        var failures = new List<string>();
        Uri? parsedPublicBaseUrl = null;

        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} is required.");
        }
        else if (!Uri.TryCreate(options.PublicBaseUrl.Trim(), UriKind.Absolute, out parsedPublicBaseUrl))
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} must be a valid absolute URL.");
        }
        else if (!string.Equals(parsedPublicBaseUrl.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsedPublicBaseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} must use http or https.");
        }

        if (_environment?.IsProduction() == true)
        {
            ValidateProductionSettings(options, parsedPublicBaseUrl, failures);
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateProductionSettings(EmailSettings options, Uri? parsedPublicBaseUrl, List<string> failures)
    {
        if (parsedPublicBaseUrl != null)
        {
            if (!string.Equals(parsedPublicBaseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} must use https in Production.");
            }

            if (parsedPublicBaseUrl.IsLoopback)
            {
                failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.PublicBaseUrl)} must be a public non-localhost URL in Production.");
            }
        }

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.Host)} is required in Production.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.Port)} must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.Username)} is required in Production. Set Render environment variable EmailSettings__Username.");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add($"{EmailSettings.SectionName}:{nameof(EmailSettings.Password)} is required in Production. Set Render environment variable EmailSettings__Password.");
        }
    }
}
