using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Configuration;

public class EmailSettingsValidator : IValidateOptions<EmailSettings>
{
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

        return ValidateOptionsResult.Success;
    }
}
