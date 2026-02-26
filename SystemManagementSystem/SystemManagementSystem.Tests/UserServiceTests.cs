using Moq;
using SystemManagementSystem.DTOs.Users;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Implementations;
using SystemManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SystemManagementSystem.Tests;

public class UserServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextMock = new();

    private UserService CreateService(Data.ApplicationDbContext context)
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextMock.Setup(x => x.HttpContext).Returns(httpContext);

        return new UserService(context, _auditLogMock.Object, _httpContextMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsUser()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var result = await svc.CreateAsync(new CreateUserRequest
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        });

        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@test.com", result.Email);
        _auditLogMock.Verify(x => x.LogAsync("Create", "User", It.IsAny<string>(), null, It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUsername_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateUserRequest { Username = "testuser", Email = "a@a.com", Password = "Pass123!", FirstName = "A", LastName = "B" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateUserRequest { Username = "testuser", Email = "b@b.com", Password = "Pass123!", FirstName = "C", LastName = "D" }));
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateUserRequest { Username = "user1", Email = "same@same.com", Password = "Pass123!", FirstName = "A", LastName = "B" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateUserRequest { Username = "user2", Email = "same@same.com", Password = "Pass123!", FirstName = "C", LastName = "D" }));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateUserRequest { Username = "findme", Email = "find@me.com", Password = "Pass123!", FirstName = "Find", LastName = "Me" });
        var result = await svc.GetByIdAsync(created.Id);

        Assert.Equal("findme", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_Updates()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateUserRequest { Username = "update", Email = "update@t.com", Password = "Pass123!", FirstName = "Old", LastName = "Name" });
        var updated = await svc.UpdateAsync(created.Id, new UpdateUserRequest { FirstName = "New" });

        Assert.Equal("New", updated.FirstName);
        _auditLogMock.Verify(x => x.LogAsync("Update", "User", created.Id.ToString(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_SoftDeletes()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateUserRequest { Username = "delete", Email = "del@t.com", Password = "Pass123!", FirstName = "Del", LastName = "Ete" });
        await svc.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
        _auditLogMock.Verify(x => x.LogAsync("Delete", "User", created.Id.ToString(), It.IsAny<string>(), null, It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        for (int i = 0; i < 5; i++)
            await svc.CreateAsync(new CreateUserRequest { Username = $"user{i}", Email = $"u{i}@t.com", Password = "Pass123!", FirstName = $"F{i}", LastName = $"L{i}" });

        var result = await svc.GetAllAsync(1, 3);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task AssignRolesAsync_ValidRoles_AssignsRoles()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var role = new Role { Name = "Admin", Description = "Admin role" };
        ctx.Roles.Add(role);
        ctx.SaveChanges();

        var created = await svc.CreateAsync(new CreateUserRequest { Username = "roleuser", Email = "role@t.com", Password = "Pass123!", FirstName = "R", LastName = "U" });
        var result = await svc.AssignRolesAsync(created.Id, new AssignRolesRequest { RoleIds = new List<Guid> { role.Id } });

        Assert.Contains("Admin", result.Roles);
    }

    [Fact]
    public async Task AssignRolesAsync_InvalidRole_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateUserRequest { Username = "norole", Email = "nr@t.com", Password = "Pass123!", FirstName = "N", LastName = "R" });

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.AssignRolesAsync(created.Id, new AssignRolesRequest { RoleIds = new List<Guid> { Guid.NewGuid() } }));
    }
}
