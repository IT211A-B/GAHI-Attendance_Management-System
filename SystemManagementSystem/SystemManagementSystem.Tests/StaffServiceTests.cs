using Moq;
using SystemManagementSystem.DTOs.Staff;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Models.Enums;
using SystemManagementSystem.Services.Implementations;
using SystemManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SystemManagementSystem.Tests;

public class StaffServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextMock = new();

    private StaffService CreateService(Data.ApplicationDbContext context)
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextMock.Setup(x => x.HttpContext).Returns(httpContext);

        return new StaffService(context, _auditLogMock.Object, _httpContextMock.Object);
    }

    private static Department SeedDepartment(Data.ApplicationDbContext ctx)
    {
        var dept = new Department { Name = "COLLEGE", Code = "COL" };
        ctx.Departments.Add(dept);
        ctx.SaveChanges();
        return dept;
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsStaff()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        var result = await svc.CreateAsync(new CreateStaffRequest
        {
            EmployeeIdNumber = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            StaffType = StaffType.Teaching,
            DepartmentId = dept.Id
        });

        Assert.Equal("EMP-001", result.EmployeeIdNumber);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Teaching", result.StaffType);
        _auditLogMock.Verify(x => x.LogAsync("Create", "Staff", It.IsAny<string>(), null, It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmployeeId_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-001", FirstName = "A", LastName = "B", StaffType = StaffType.Teaching, DepartmentId = dept.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-001", FirstName = "C", LastName = "D", StaffType = StaffType.Teaching, DepartmentId = dept.Id }));
    }

    [Fact]
    public async Task CreateAsync_InvalidDepartment_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedDepartment(ctx);
        var svc = CreateService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-002", FirstName = "A", LastName = "B", StaffType = StaffType.Teaching, DepartmentId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStaff_ReturnsStaff()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-003", FirstName = "Jane", LastName = "Smith", StaffType = StaffType.NonTeaching, DepartmentId = dept.Id });
        var result = await svc.GetByIdAsync(created.Id);

        Assert.Equal("EMP-003", result.EmployeeIdNumber);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesAndAudits()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-004", FirstName = "Bob", LastName = "Jones", StaffType = StaffType.Teaching, DepartmentId = dept.Id });
        var updated = await svc.UpdateAsync(created.Id, new UpdateStaffRequest { FirstName = "Bobby" });

        Assert.Equal("Bobby", updated.FirstName);
        _auditLogMock.Verify(x => x.LogAsync("Update", "Staff", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingStaff_SoftDeletes()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-005", FirstName = "Del", LastName = "Ete", StaffType = StaffType.Security, DepartmentId = dept.Id });
        await svc.DeleteAsync(created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
        _auditLogMock.Verify(x => x.LogAsync("Delete", "Staff", It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        for (int i = 0; i < 5; i++)
            await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = $"EMP-{i:D3}", FirstName = $"Staff{i}", LastName = "Test", StaffType = StaffType.Teaching, DepartmentId = dept.Id });

        var result = await svc.GetAllAsync(1, 3, null, null);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchFilter_FiltersCorrectly()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-A", FirstName = "Alpha", LastName = "Test", StaffType = StaffType.Teaching, DepartmentId = dept.Id });
        await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-B", FirstName = "Beta", LastName = "Test", StaffType = StaffType.Teaching, DepartmentId = dept.Id });

        var result = await svc.GetAllAsync(1, 10, null, "Alpha");
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task RegenerateQrCodeAsync_GeneratesNewQr()
    {
        using var ctx = TestDbContextFactory.Create();
        var dept = SeedDepartment(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStaffRequest { EmployeeIdNumber = "EMP-QR", FirstName = "Qr", LastName = "Test", StaffType = StaffType.Teaching, DepartmentId = dept.Id });
        var regenerated = await svc.RegenerateQrCodeAsync(created.Id);

        Assert.NotEqual(created.QrCodeData, regenerated.QrCodeData);
        Assert.StartsWith("EMP-QR-", regenerated.QrCodeData);
    }
}
