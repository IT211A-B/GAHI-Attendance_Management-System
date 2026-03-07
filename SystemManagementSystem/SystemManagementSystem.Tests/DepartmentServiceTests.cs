using SystemManagementSystem.DTOs.Departments;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Implementations;
using SystemManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace SystemManagementSystem.Tests;

public class DepartmentServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextMock = new();

    private DepartmentService CreateService(Data.ApplicationDbContext context)
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextMock.Setup(x => x.HttpContext).Returns(httpContext);

        return new DepartmentService(context, _auditLogMock.Object, _httpContextMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDepartment()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var result = await svc.CreateAsync(new CreateDepartmentRequest { Name = "Engineering", Code = "ENG", Description = "Engineering Dept" });

        Assert.Equal("Engineering", result.Name);
        Assert.Equal("ENG", result.Code);
        _auditLogMock.Verify(x => x.LogAsync("Create", "Department", It.IsAny<string>(), null, It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateDepartmentRequest { Name = "Engineering", Code = "ENG" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateDepartmentRequest { Name = "Engineering2", Code = "ENG" }));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingDepartment_ReturnsDepartment()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateDepartmentRequest { Name = "Science", Code = "SCI" });
        var result = await svc.GetByIdAsync(created.Id);

        Assert.Equal("Science", result.Name);
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

        var created = await svc.CreateAsync(new CreateDepartmentRequest { Name = "Math", Code = "MATH" });
        var updated = await svc.UpdateAsync(created.Id, new UpdateDepartmentRequest { Name = "Mathematics" });

        Assert.Equal("Mathematics", updated.Name);
        _auditLogMock.Verify(x => x.LogAsync("Update", "Department", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingDepartment_SoftDeletes()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateDepartmentRequest { Name = "ToDelete", Code = "DEL" });
        await svc.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
        _auditLogMock.Verify(x => x.LogAsync("Delete", "Department", It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var svc = CreateService(ctx);

        for (int i = 0; i < 5; i++)
            await svc.CreateAsync(new CreateDepartmentRequest { Name = $"Dept{i}", Code = $"D{i}" });

        var result = await svc.GetAllAsync(1, 3);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }
}
