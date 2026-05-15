using System.Net;

namespace Attendance_Management_System.Tests;

public class SecurityHeaderTests
{
    [Fact]
    public async Task SecurityHeaders_AllowStudentQrCameraAndScannerCdn()
    {
        using var factory = new SecurityHeaderWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/__headers_probe__");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var contentSecurityPolicy = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net", contentSecurityPolicy);

        var permissionsPolicy = Assert.Single(response.Headers.GetValues("Permissions-Policy"));
        Assert.Contains("camera=(self)", permissionsPolicy);
        Assert.DoesNotContain("camera=()", permissionsPolicy);
    }
}

internal sealed class SecurityHeaderWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
