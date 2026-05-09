using System.Net;

namespace Attendance_Management_System.Tests;

public class SectionAttendanceRedirectTests
{
    [Fact]
    public async Task AttendanceIndex_RedirectsToChecklistRoute_WithPreservedQueryValues()
    {
        await using var factory = new RateLimitingWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/attendance?sectionId=7&scheduleId=9&date=2026-05-09");
        request.Headers.Add(TestAuthHandler.UserIdHeader, "101");
        request.Headers.Add(TestAuthHandler.RoleHeader, "teacher");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var location = response.Headers.Location!.ToString();
        Assert.Contains("/attendance/checklist", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sectionId=7", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scheduleId=9", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("attendanceDate=2026-05-09", location, StringComparison.OrdinalIgnoreCase);
    }
}
