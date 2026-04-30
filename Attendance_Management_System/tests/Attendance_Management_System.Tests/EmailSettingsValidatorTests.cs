using Attendance_Management_System.Backend.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Tests;

public class EmailSettingsValidatorTests
{
    [Fact]
    public void Validate_ReturnsFailure_WhenPublicBaseUrlMissing()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, new EmailSettings { PublicBaseUrl = "" });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains(nameof(EmailSettings.PublicBaseUrl), StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenProductionUrlIsNotHttps()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, new EmailSettings { PublicBaseUrl = "http://attendance.example.edu" });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("https", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenProductionUrlIsLoopback()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, new EmailSettings { PublicBaseUrl = "https://localhost:7050" });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, failure => failure.Contains("loopback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenProductionUrlIsHttpsAndPublic()
    {
        var validator = CreateValidator(Environments.Production);

        var result = validator.Validate(Options.DefaultName, new EmailSettings { PublicBaseUrl = "https://attendance.example.edu" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenDevelopmentUsesLocalhostHttps()
    {
        var validator = CreateValidator(Environments.Development);

        var result = validator.Validate(Options.DefaultName, new EmailSettings { PublicBaseUrl = "https://localhost:7050" });

        Assert.True(result.Succeeded);
    }

    private static EmailSettingsValidator CreateValidator(string environmentName)
    {
        return new EmailSettingsValidator(new TestHostEnvironment
        {
            EnvironmentName = environmentName
        });
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Attendance_Management_System.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
