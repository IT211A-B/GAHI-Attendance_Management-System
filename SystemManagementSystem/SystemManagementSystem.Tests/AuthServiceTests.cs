using Microsoft.Extensions.Configuration;
using SystemManagementSystem.DTOs.Auth;
using SystemManagementSystem.Helpers;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Implementations;

namespace SystemManagementSystem.Tests;

public class AuthServiceTests
{
    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "DBTC-IAS-SuperSecretKey-Change-In-Production-2026!",
                ["JwtSettings:Issuer"] = "DBTC-IAS",
                ["JwtSettings:Audience"] = "DBTC-IAS-Clients",
                ["JwtSettings:ExpirationInMinutes"] = "480"
            })
            .Build();

    private static (Data.ApplicationDbContext ctx, User user, Role role) SeedUserWithRole()
    {
        var ctx = TestDbContextFactory.Create();
        var role = new Role { Name = "Admin", Description = "Admin role" };
        ctx.Roles.Add(role);

        var user = new User
        {
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "Test",
            LastName = "User",
            IsActive = true
        };
        ctx.Users.Add(user);

        ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        ctx.SaveChanges();

        return (ctx, user, role);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndRefreshToken()
    {
        var (ctx, user, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        var result = await svc.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });

        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.RefreshToken);
        Assert.Equal("testuser", result.Username);
        Assert.Contains("Admin", result.Roles);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);

        ctx.Dispose();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorized()
    {
        var (ctx, _, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest { Username = "testuser", Password = "WrongPassword" }));

        ctx.Dispose();
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ThrowsUnauthorized()
    {
        var (ctx, _, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest { Username = "nouser", Password = "Password123!" }));

        ctx.Dispose();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorized()
    {
        var ctx = TestDbContextFactory.Create();
        var user = new User
        {
            Username = "inactive",
            Email = "inactive@t.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FirstName = "In",
            LastName = "Active",
            IsActive = false
        };
        ctx.Users.Add(user);
        ctx.SaveChanges();

        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.LoginAsync(new LoginRequest { Username = "inactive", Password = "Password123!" }));

        ctx.Dispose();
    }

    [Fact]
    public async Task RefreshAsync_ValidToken_ReturnsNewTokens()
    {
        var (ctx, user, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        var loginResult = await svc.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });
        var refreshResult = await svc.RefreshAsync(loginResult.RefreshToken);

        Assert.NotEmpty(refreshResult.Token);
        Assert.NotEmpty(refreshResult.RefreshToken);
        Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);
        Assert.Equal("testuser", refreshResult.Username);

        ctx.Dispose();
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ThrowsUnauthorized()
    {
        var (ctx, _, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.RefreshAsync("invalid-token"));

        ctx.Dispose();
    }

    [Fact]
    public async Task RefreshAsync_UsedToken_ThrowsUnauthorized()
    {
        var (ctx, _, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        var loginResult = await svc.LoginAsync(new LoginRequest { Username = "testuser", Password = "Password123!" });
        // Use the refresh token once
        await svc.RefreshAsync(loginResult.RefreshToken);
        // Try to use it again (should be revoked)
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.RefreshAsync(loginResult.RefreshToken));

        ctx.Dispose();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ChangesPassword()
    {
        var (ctx, user, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await svc.ChangePasswordAsync(user.Id, new ChangePasswordRequest
        {
            CurrentPassword = "Password123!",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        });

        // Verify login works with new password
        var result = await svc.LoginAsync(new LoginRequest { Username = "testuser", Password = "NewPassword456!" });
        Assert.NotEmpty(result.Token);

        ctx.Dispose();
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsUnauthorized()
    {
        var (ctx, user, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.ChangePasswordAsync(user.Id, new ChangePasswordRequest
            {
                CurrentPassword = "WrongPassword",
                NewPassword = "NewPassword456!",
                ConfirmPassword = "NewPassword456!"
            }));

        ctx.Dispose();
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistentUser_ThrowsKeyNotFound()
    {
        var (ctx, _, _) = SeedUserWithRole();
        var config = CreateConfig();
        var jwtHelper = new JwtTokenHelper(config);
        var svc = new AuthService(ctx, jwtHelper, config);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.ChangePasswordAsync(Guid.NewGuid(), new ChangePasswordRequest
            {
                CurrentPassword = "Password123!",
                NewPassword = "NewPassword456!",
                ConfirmPassword = "NewPassword456!"
            }));

        ctx.Dispose();
    }
}
