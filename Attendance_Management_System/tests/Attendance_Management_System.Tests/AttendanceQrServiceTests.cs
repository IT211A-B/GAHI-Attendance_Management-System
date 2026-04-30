using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Attendance_Management_System.Tests;

public class AttendanceQrServiceTests
{
    [Fact]
    public async Task SearchSectionsAsync_ReturnsAllAssignedSections_NotOnlyTodaySchedules()
    {
        await using var context = CreateContext();
        SeedLookupRows(context);

        var teacher = new Teacher
        {
            Id = 10,
            UserId = 100,
            EmployeeNumber = "T-100",
            FirstName = "Ada",
            LastName = "Teacher",
            Department = "College"
        };

        var sectionA = CreateSection(1, "Section Alpha");
        var sectionB = CreateSection(2, "Section Beta");
        var sectionC = CreateSection(3, "Section Gamma");

        context.Teachers.Add(teacher);
        context.Sections.AddRange(sectionA, sectionB, sectionC);

        context.SectionTeachers.AddRange(
            new SectionTeacher { SectionId = sectionA.Id, TeacherId = teacher.Id },
            new SectionTeacher { SectionId = sectionB.Id, TeacherId = teacher.Id });

        // Section C is included through schedule ownership even without explicit section assignment.
        context.Schedules.Add(new Schedule
        {
            Id = 50,
            SectionId = sectionC.Id,
            TeacherId = teacher.Id,
            SubjectId = 1,
            DayOfWeek = (int)DayOfWeek.Sunday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2026, 12, 31)
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.SearchSectionsAsync(teacher.UserId, "teacher", query: null, take: 8);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(3, response.Data!.Count);
        Assert.Collection(response.Data,
            item => Assert.Equal("Section Alpha", item.SectionName),
            item => Assert.Equal("Section Beta", item.SectionName),
            item => Assert.Equal("Section Gamma", item.SectionName));
    }

    [Fact]
    public async Task SearchSectionsAsync_AppliesQueryFilter_AndTakeLimit()
    {
        await using var context = CreateContext();
        SeedLookupRows(context);

        var teacher = new Teacher
        {
            Id = 11,
            UserId = 101,
            EmployeeNumber = "T-101",
            FirstName = "Grace",
            LastName = "Teacher",
            Department = "College"
        };

        var alpha = CreateSection(11, "Alpha Section");
        var alpine = CreateSection(12, "Alpine Section");
        var beta = CreateSection(13, "Beta Section");

        context.Teachers.Add(teacher);
        context.Sections.AddRange(alpha, alpine, beta);
        context.SectionTeachers.AddRange(
            new SectionTeacher { SectionId = alpha.Id, TeacherId = teacher.Id },
            new SectionTeacher { SectionId = alpine.Id, TeacherId = teacher.Id },
            new SectionTeacher { SectionId = beta.Id, TeacherId = teacher.Id });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.SearchSectionsAsync(teacher.UserId, "teacher", query: "Al", take: 1);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data!);
        Assert.Equal("Alpha Section", response.Data![0].SectionName);
    }

    [Fact]
    public async Task SearchSectionsAsync_ReturnsForbidden_WhenRoleIsNotTeacherOrAdmin()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.SearchSectionsAsync(999, "student", query: null, take: 8);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Forbidden, response.Error!.Code);
    }

    [Fact]
    public async Task SearchSectionsAsync_ReturnsForbidden_WhenTeacherProfileMissing()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.SearchSectionsAsync(404, "teacher", query: null, take: 8);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Forbidden, response.Error!.Code);
    }

    [Fact]
    public async Task CloseSessionAsync_ClosesOwnedActiveSession()
    {
        await using var context = CreateContext();

        var teacher = new Teacher
        {
            Id = 21,
            UserId = 210,
            EmployeeNumber = "T-210",
            FirstName = "Ada",
            LastName = "Owner",
            Department = "College"
        };

        var session = new AttendanceQrSession
        {
            Id = 1,
            SessionId = "qrs-active",
            SectionId = 1,
            ScheduleId = 1,
            SubjectId = 1,
            CreatedByUserId = teacher.UserId,
            OwnerTeacherId = teacher.Id,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            IsActive = true,
            TokenNonce = "nonce-active",
            ClosedAtUtc = null
        };

        context.Teachers.Add(teacher);
        context.AttendanceQrSessions.Add(session);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.CloseSessionAsync(teacher.UserId, "teacher", session.SessionId);

        Assert.True(response.Success);
        Assert.True(response.Data);

        var updatedSession = await context.AttendanceQrSessions
            .AsNoTracking()
            .FirstAsync(item => item.SessionId == session.SessionId);

        Assert.False(updatedSession.IsActive);
        Assert.NotNull(updatedSession.ClosedAtUtc);
    }

    [Fact]
    public async Task CloseSessionAsync_IsIdempotent_WhenSessionAlreadyClosed()
    {
        await using var context = CreateContext();

        var teacher = new Teacher
        {
            Id = 22,
            UserId = 220,
            EmployeeNumber = "T-220",
            FirstName = "Grace",
            LastName = "Owner",
            Department = "College"
        };

        var closedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2);
        var session = new AttendanceQrSession
        {
            Id = 2,
            SessionId = "qrs-closed",
            SectionId = 1,
            ScheduleId = 1,
            SubjectId = 1,
            CreatedByUserId = teacher.UserId,
            OwnerTeacherId = teacher.Id,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            IsActive = false,
            TokenNonce = "nonce-closed",
            ClosedAtUtc = closedAtUtc
        };

        context.Teachers.Add(teacher);
        context.AttendanceQrSessions.Add(session);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.CloseSessionAsync(teacher.UserId, "teacher", session.SessionId);

        Assert.True(response.Success);
        Assert.True(response.Data);

        var updatedSession = await context.AttendanceQrSessions
            .AsNoTracking()
            .FirstAsync(item => item.SessionId == session.SessionId);

        Assert.False(updatedSession.IsActive);
        Assert.Equal(closedAtUtc, updatedSession.ClosedAtUtc);
    }

    [Fact]
    public async Task CloseSessionAsync_ReturnsForbidden_WhenCallerDoesNotOwnSession()
    {
        await using var context = CreateContext();

        var ownerTeacher = new Teacher
        {
            Id = 23,
            UserId = 230,
            EmployeeNumber = "T-230",
            FirstName = "Owner",
            LastName = "Teacher",
            Department = "College"
        };

        var otherTeacher = new Teacher
        {
            Id = 24,
            UserId = 240,
            EmployeeNumber = "T-240",
            FirstName = "Other",
            LastName = "Teacher",
            Department = "College"
        };

        var session = new AttendanceQrSession
        {
            Id = 3,
            SessionId = "qrs-owner",
            SectionId = 1,
            ScheduleId = 1,
            SubjectId = 1,
            CreatedByUserId = ownerTeacher.UserId,
            OwnerTeacherId = ownerTeacher.Id,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-4),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(4),
            IsActive = true,
            TokenNonce = "nonce-owner",
            ClosedAtUtc = null
        };

        context.Teachers.AddRange(ownerTeacher, otherTeacher);
        context.AttendanceQrSessions.Add(session);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.CloseSessionAsync(otherTeacher.UserId, "teacher", session.SessionId);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Forbidden, response.Error!.Code);
    }

    private static AttendanceQrService CreateService(AppDbContext context)
    {
        var attendanceServiceMock = new Mock<IAttendanceService>(MockBehavior.Strict);
        var notificationServiceMock = new Mock<INotificationService>(MockBehavior.Loose);

        return new AttendanceQrService(
            context,
            attendanceServiceMock.Object,
            Options.Create(AttendanceSettings.Default),
            Options.Create(AttendanceQrSettings.Default),
            notificationServiceMock.Object,
            NullLogger<AttendanceQrService>.Instance);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedLookupRows(AppDbContext context)
    {
        context.AcademicYears.Add(new AcademicYear
        {
            Id = 1,
            YearLabel = "2025-2026",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2026, 5, 31),
            IsActive = true
        });

        context.Courses.Add(new Course
        {
            Id = 1,
            Name = "Computer Science",
            Code = "BSCS"
        });

        context.Subjects.Add(new Subject
        {
            Id = 1,
            Name = "Algorithms",
            Code = "CS101",
            CourseId = 1
        });

        context.Classrooms.Add(new Classroom
        {
            Id = 1,
            Name = "Room 1"
        });
    }

    private static Section CreateSection(int id, string name)
    {
        return new Section
        {
            Id = id,
            Name = name,
            YearLevel = 1,
            AcademicYearId = 1,
            CourseId = 1,
            SubjectId = 1,
            ClassroomId = 1
        };
    }
}

public class RateLimitingIntegrationTests
{
    [Fact]
    public async Task LoginPost_IsThrottled_AfterPolicyLimit()
    {
        await using var factory = new RateLimitingWebApplicationFactory();
        using var client = CreateClient(factory);
        using var throttledResponse = await SendUntilThrottledAsync(
            async () =>
            {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string>());
                return await client.PostAsync("/login", content);
            },
            maxAttempts: 130);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttledResponse.StatusCode);
    }

    [Fact]
    public async Task AuthLoginPolicy_UsesSeparateBuckets_PerAuthenticatedUser()
    {
        await using var factory = new RateLimitingWebApplicationFactory();
        using var client = CreateClient(factory);

        using var userOneThrottledResponse = await SendUntilThrottledAsync(
            async () =>
            {
                using var request = CreateLoginPostRequest(userId: "101", role: "teacher");
                return await client.SendAsync(request);
            },
            maxAttempts: 130);
        Assert.Equal(HttpStatusCode.TooManyRequests, userOneThrottledResponse.StatusCode);

        using var userTwoRequest = CreateLoginPostRequest(userId: "202", role: "teacher");
        using var userTwoFirstResponse = await client.SendAsync(userTwoRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, userTwoFirstResponse.StatusCode);
    }

    [Fact]
    public async Task ThrottledQrApiRoute_ReturnsApiResponsePayload_AndRetryAfter()
    {
        await using var factory = new RateLimitingWebApplicationFactory();
        using var client = CreateClient(factory);
        using var throttledResponse = await SendUntilThrottledAsync(
            async () =>
            {
                using var request = CreateQrCheckinRequest(userId: "501", role: "student");
                return await client.SendAsync(request);
            },
            maxAttempts: 130);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttledResponse.StatusCode);
        Assert.True(throttledResponse.Headers.TryGetValues("Retry-After", out var retryAfterValues));
        Assert.True(int.TryParse(retryAfterValues.Single(), out var retryAfterSeconds));
        Assert.True(retryAfterSeconds > 0);

        var payload = await throttledResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.NotNull(payload.Error);
        Assert.Equal(ErrorCodes.TooManyRequests, payload.Error!.Code);
    }

    [Fact]
    public async Task GlobalLimiter_UsesSeparateBuckets_PerAuthenticatedUser()
    {
        await using var factory = new RateLimitingWebApplicationFactory();
        using var client = CreateClient(factory);
        using var userOneThrottledResponse = await SendUntilThrottledAsync(
            async () =>
            {
                using var request = CreateAuthenticatedRequest("/attendance", "101", "teacher");
                return await client.SendAsync(request);
            },
            maxAttempts: 130);
        Assert.Equal(HttpStatusCode.TooManyRequests, userOneThrottledResponse.StatusCode);

        using var userTwoRequest = CreateAuthenticatedRequest("/attendance", "202", "teacher");
        using var userTwoFirstResponse = await client.SendAsync(userTwoRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, userTwoFirstResponse.StatusCode);
    }

    [Fact]
    public async Task QrCheckinPolicy_UsesSeparateBuckets_PerAuthenticatedUser()
    {
        await using var factory = new RateLimitingWebApplicationFactory();
        using var client = CreateClient(factory);

        using var userOneThrottledResponse = await SendUntilThrottledAsync(
            async () =>
            {
                using var request = CreateQrCheckinRequest(userId: "301", role: "student");
                return await client.SendAsync(request);
            },
            maxAttempts: 130);
        Assert.Equal(HttpStatusCode.TooManyRequests, userOneThrottledResponse.StatusCode);

        using var userTwoRequest = CreateQrCheckinRequest(userId: "302", role: "student");
        using var userTwoFirstResponse = await client.SendAsync(userTwoRequest);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, userTwoFirstResponse.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendUntilThrottledAsync(
        Func<Task<HttpResponseMessage>> sendRequest,
        int maxAttempts)
    {
        HttpResponseMessage? lastResponse = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var response = await sendRequest();
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                lastResponse?.Dispose();
                return response;
            }

            lastResponse?.Dispose();
            lastResponse = response;
        }

        var lastStatus = lastResponse?.StatusCode.ToString() ?? "no response";
        lastResponse?.Dispose();
        throw new Xunit.Sdk.XunitException(
            $"Expected a 429 TooManyRequests within {maxAttempts} attempts, but the last status was {lastStatus}.");
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static HttpRequestMessage CreateQrCheckinRequest(string userId, string role)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/attendance/qr/checkins")
        {
            Content = JsonContent.Create(new { token = "sample-token" })
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RoleHeader, role);
        return request;
    }

    private static HttpRequestMessage CreateLoginPostRequest(string userId, string role)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>())
        };

        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RoleHeader, role);
        return request;
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(string path, string userId, string role)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        request.Headers.Add(TestAuthHandler.RoleHeader, role);
        return request;
    }

}

internal sealed class RateLimitingWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RateLimitingSettings.SectionName}:Global:PermitLimit"] = "5",
                [$"{RateLimitingSettings.SectionName}:AuthLogin:PermitLimit"] = "3",
                [$"{RateLimitingSettings.SectionName}:QrCheckins:PermitLimit"] = "4"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = userIdValues.ToString();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = Request.Headers.TryGetValue(RoleHeader, out var roleValues)
            ? roleValues.ToString()
            : "teacher";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, $"test-user-{userId}"),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}