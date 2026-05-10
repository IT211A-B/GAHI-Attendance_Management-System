namespace Attendance_Management_System.Tests;

public class EmailSettingsValidatorTests
{
    [Fact]
    public void Validate_ReturnsFailure_WhenPublicBaseUrlMissing()
    {
        var validator = CreateValidator();

        var result = validator.Validate(Options.DefaultName, CreateValidSettings(""));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(EmailSettings.PublicBaseUrl), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenUrlIsNotAbsolute()
    {
        var validator = CreateValidator();

        var result = validator.Validate(Options.DefaultName, CreateValidSettings("attendance.example.edu"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("absolute URL", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenUrlSchemeIsNotHttpOrHttps()
    {
        var validator = CreateValidator();

        var result = validator.Validate(Options.DefaultName, CreateValidSettings("ftp://attendance.example.edu"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("http or https", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenUrlUsesHttp()
    {
        var validator = CreateValidator();

        var result = validator.Validate(Options.DefaultName, CreateValidSettings("http://attendance.example.edu"));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenUrlUsesHttpsLocalhost()
    {
        var validator = CreateValidator();

        var result = validator.Validate(Options.DefaultName, CreateValidSettings("https://localhost:7050"));

        Assert.True(result.Succeeded);
    }

    private static EmailSettings CreateValidSettings(string publicBaseUrl)
    {
        return new EmailSettings
        {
            PublicBaseUrl = publicBaseUrl
        };
    }

    private static EmailSettingsValidator CreateValidator()
    {
        return new EmailSettingsValidator();
    }
}
