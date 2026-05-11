using Microsoft.AspNetCore.Authentication.Cookies;

namespace Attendance_Management_System.Tests;

public class CookieConfigurationTests
{
    [Fact]
    public void ApplicationCookie_UsesConfiguredCookieSettings()
    {
        using var factory = new CookieConfigurationWebApplicationFactory();

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal(TimeSpan.FromHours(6), options.ExpireTimeSpan);
        Assert.False(options.SlidingExpiration);
        Assert.False(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
    }

    [Fact]
    public async Task ApplicationCookie_ReturnsStatusCodes_ForQrApiAuthRedirects()
    {
        using var factory = new CookieConfigurationWebApplicationFactory();
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        var loginContext = CreateRedirectContext("/attendance/qr/options/sections", options);
        await options.Events.OnRedirectToLogin(loginContext);
        Assert.Equal(StatusCodes.Status401Unauthorized, loginContext.Response.StatusCode);
        Assert.False(loginContext.Response.Headers.ContainsKey("Location"));

        var accessDeniedContext = CreateRedirectContext("/attendance/qr/sessions/session-1/checkins", options);
        await options.Events.OnRedirectToAccessDenied(accessDeniedContext);
        Assert.Equal(StatusCodes.Status403Forbidden, accessDeniedContext.Response.StatusCode);
        Assert.False(accessDeniedContext.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task ApplicationCookie_RedirectsBrowserQrPage_ToLogin()
    {
        using var factory = new CookieConfigurationWebApplicationFactory();
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        var context = CreateRedirectContext("/attendance/qr", options);
        await options.Events.OnRedirectToLogin(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/login", context.Response.Headers.Location);
    }

    private static RedirectContext<CookieAuthenticationOptions> CreateRedirectContext(
        string path,
        CookieAuthenticationOptions options)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var scheme = new AuthenticationScheme(
            IdentityConstants.ApplicationScheme,
            IdentityConstants.ApplicationScheme,
            typeof(CookieAuthenticationHandler));

        return new RedirectContext<CookieAuthenticationOptions>(
            httpContext,
            scheme,
            options,
            new AuthenticationProperties(),
            "/login");
    }
}

internal sealed class CookieConfigurationWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{CookieSettings.SectionName}:ExpirationHours"] = "6",
                [$"{CookieSettings.SectionName}:SlidingExpiration"] = "false",
                [$"{CookieSettings.SectionName}:HttpOnly"] = "false",
                [$"{CookieSettings.SectionName}:SameSite"] = "Strict",
                [$"{CookieSettings.SectionName}:SecurePolicy"] = "Always"
            });
        });
    }
}
