using System.Text;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Attendance_Management_System.Tests;

public class AuthServiceEmailConfirmationTests
{
    [Fact]
    public async Task ResendVerificationAsync_UsesConfiguredPublicBaseUrl()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager();
        var accountEmailServiceMock = new Mock<IAccountEmailService>(MockBehavior.Strict);

        var user = new User
        {
            Id = 64,
            Email = "student@example.com",
            IsActive = true,
            EmailConfirmed = false
        };

        const string rawToken = "Token+/With=Symbols";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        string? capturedLink = null;

        userManager.FindByEmailHandler = _ => Task.FromResult<User?>(user);
        userManager.GenerateTokenHandler = _ => Task.FromResult(rawToken);

        accountEmailServiceMock
            .Setup(service => service.SendVerificationEmailAsync(user.Email!, user.Email!, It.IsAny<string>()))
            .Callback<string, string, string>((_, _, confirmationLink) => capturedLink = confirmationLink)
            .Returns(Task.CompletedTask);

        var service = CreateService(
            context,
            userManager,
            accountEmailServiceMock,
            new EmailSettings { PublicBaseUrl = "https://attendance.example.edu" });

        var result = await service.ResendVerificationAsync(user.Email!);

        Assert.True(result.Success);
        Assert.Equal("If an account exists for that email, a verification link has been sent. Please check your inbox.", result.Message);
        Assert.Equal($"https://attendance.example.edu/confirm-email?userId={user.Id}&token={encodedToken}", capturedLink);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ConfirmsUser_WhenTokenIsValid()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager();
        var accountEmailServiceMock = new Mock<IAccountEmailService>(MockBehavior.Strict);

        var user = new User { Id = 9, EmailConfirmed = false };
        const string rawToken = "MyConfirmationToken+/=";
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        userManager.FindByIdHandler = _ => Task.FromResult<User?>(user);
        userManager.ConfirmEmailHandler = (_, _) => Task.FromResult(IdentityResult.Success);

        var service = CreateService(context, userManager, accountEmailServiceMock);
        var result = await service.ConfirmEmailAsync(user.Id, encodedToken);

        Assert.True(result.Success);
        Assert.Equal("Email confirmed successfully. You can now sign in.", result.Message);
        Assert.Equal(1, userManager.ConfirmEmailCallCount);
        Assert.Equal(rawToken, userManager.LastConfirmedToken);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsFailure_WhenTokenIsInvalid()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager();
        var accountEmailServiceMock = new Mock<IAccountEmailService>(MockBehavior.Strict);

        var user = new User { Id = 13, EmailConfirmed = false };
        userManager.FindByIdHandler = _ => Task.FromResult<User?>(user);

        var service = CreateService(context, userManager, accountEmailServiceMock);
        var result = await service.ConfirmEmailAsync(user.Id, "%%%INVALID%%%TOKEN%%%");

        Assert.False(result.Success);
        Assert.Equal("Invalid or expired email confirmation link.", result.Message);
        Assert.Equal(0, userManager.ConfirmEmailCallCount);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsSuccess_WhenAlreadyConfirmed()
    {
        await using var context = CreateContext();
        var userManager = CreateUserManager();
        var accountEmailServiceMock = new Mock<IAccountEmailService>(MockBehavior.Strict);

        var user = new User { Id = 22, EmailConfirmed = true };
        userManager.FindByIdHandler = _ => Task.FromResult<User?>(user);

        var service = CreateService(context, userManager, accountEmailServiceMock);
        var result = await service.ConfirmEmailAsync(user.Id, "any-token");

        Assert.True(result.Success);
        Assert.Equal("Email is already confirmed. You can sign in.", result.Message);
        Assert.Equal(0, userManager.ConfirmEmailCallCount);
    }

    private static AuthService CreateService(
        AppDbContext context,
        FakeUserManager userManager,
        Mock<IAccountEmailService> accountEmailServiceMock,
        EmailSettings? emailSettings = null)
    {
        var sectionAllocationServiceMock = new Mock<ISectionAllocationService>(MockBehavior.Strict);
        var notificationServiceMock = new Mock<INotificationService>(MockBehavior.Strict);

        return new AuthService(
            userManager,
            context,
            sectionAllocationServiceMock.Object,
            accountEmailServiceMock.Object,
            notificationServiceMock.Object,
            Options.Create(emailSettings ?? new EmailSettings { PublicBaseUrl = "https://localhost:7050" }),
            NullLogger<AuthService>.Instance);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static FakeUserManager CreateUserManager()
    {
        return new FakeUserManager();
    }

    private sealed class FakeUserManager : UserManager<User>
    {
        public Func<string, Task<User?>> FindByEmailHandler { get; set; } = _ => Task.FromResult<User?>(null);

        public Func<string, Task<User?>> FindByIdHandler { get; set; } = _ => Task.FromResult<User?>(null);

        public Func<User, Task<string>> GenerateTokenHandler { get; set; } = _ => Task.FromResult(string.Empty);

        public Func<User, string, Task<IdentityResult>> ConfirmEmailHandler { get; set; } =
            (_, _) => Task.FromResult(IdentityResult.Failed());

        public int ConfirmEmailCallCount { get; private set; }

        public string? LastConfirmedToken { get; private set; }

        public FakeUserManager()
            : base(
                new Mock<IUserStore<User>>(MockBehavior.Strict).Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!)
        {
        }

        public override Task<User?> FindByEmailAsync(string email)
        {
            return FindByEmailHandler(email);
        }

        public override Task<User?> FindByIdAsync(string userId)
        {
            return FindByIdHandler(userId);
        }

        public override Task<string> GenerateEmailConfirmationTokenAsync(User user)
        {
            return GenerateTokenHandler(user);
        }

        public override Task<IdentityResult> ConfirmEmailAsync(User user, string token)
        {
            ConfirmEmailCallCount++;
            LastConfirmedToken = token;
            return ConfirmEmailHandler(user, token);
        }
    }
}
