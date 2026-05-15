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
    public void Validate_ReturnsFailure_InProduction_WhenUrlIsNotHttps()
    {
        var validator = CreateValidator(Environments.Production);

        var settings = CreateProductionSettings();
        settings.PublicBaseUrl = "http://attendance.example.edu";

        var result = validator.Validate(Options.DefaultName, settings);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("must use https in Production", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsFailure_InProduction_WhenUrlIsLocalhost()
    {
        var validator = CreateValidator(Environments.Production);

        var settings = CreateProductionSettings();
        settings.PublicBaseUrl = "https://localhost:7050";

        var result = validator.Validate(Options.DefaultName, settings);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("non-localhost URL in Production", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsFailure_InProduction_WhenSmtpCredentialsAreMissing()
    {
        var validator = CreateValidator(Environments.Production);

        var settings = CreateProductionSettings();
        settings.Username = "";
        settings.Password = "";

        var result = validator.Validate(Options.DefaultName, settings);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(EmailSettings.Username), StringComparison.Ordinal));
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(EmailSettings.Password), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReturnsSuccess_InProduction_WhenSmtpSettingsAreComplete()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, CreateProductionSettings());

        Assert.True(result.Succeeded);
    }

    private static EmailSettings CreateValidSettings(string publicBaseUrl)
    {
        return new EmailSettings
        {
            PublicBaseUrl = publicBaseUrl
        };
    }

    private static EmailSettings CreateProductionSettings()
    {
        return new EmailSettings
        {
            PublicBaseUrl = "https://attendance.example.edu",
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "sender@example.edu",
            Password = "smtp-app-password",
            FromName = "Don Bosco Attendance",
            UseSsl = false
        };
    }

    private static EmailSettingsValidator CreateValidator()
    {
        return new EmailSettingsValidator();
    }

    private static EmailSettingsValidator CreateValidator(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(environmentName);

        return new EmailSettingsValidator(environment.Object);
    }
}
