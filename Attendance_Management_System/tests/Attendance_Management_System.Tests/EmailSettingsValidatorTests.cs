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

    [Fact]
    public void Validate_ReturnsFailure_WhenOnlyUsernameProvided()
    {
        var validator = CreateValidator();

        var result = validator.Validate(Options.DefaultName, new EmailSettings
        {
            PublicBaseUrl = "https://attendance.example.edu",
            Username = "sender@example.edu"
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(EmailSettings.Username), StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(EmailSettings.Password), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenProductionCredentialsMissing()
    {
        var validator = CreateProductionValidator();

        var result = validator.Validate(Options.DefaultName, CreateValidSettings("https://attendance.example.edu"));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("required in Production", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenProductionCredentialsProvided()
    {
        var validator = CreateProductionValidator();

        var result = validator.Validate(Options.DefaultName, new EmailSettings
        {
            PublicBaseUrl = "https://attendance.example.edu",
            Username = "sender@example.edu",
            Password = "smtp-secret"
        });

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

    private static EmailSettingsValidator CreateProductionValidator()
    {
        return new EmailSettingsValidator(new FakeHostEnvironment
        {
            EnvironmentName = Microsoft.Extensions.Hosting.Environments.Production
        });
    }

    private sealed class FakeHostEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Microsoft.Extensions.Hosting.Environments.Development;

        public string ApplicationName { get; set; } = "Attendance_Management_System.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    }
}
