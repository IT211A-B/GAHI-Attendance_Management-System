using Moq;
using SystemManagementSystem.DTOs.Students;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Models.Enums;
using SystemManagementSystem.Services.Implementations;
using SystemManagementSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace SystemManagementSystem.Tests;

public class StudentServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextMock = new();

    private StudentService CreateService(Data.ApplicationDbContext context)
    {
        SetupHttpContext();
        return new StudentService(context, _auditLogMock.Object, _httpContextMock.Object);
    }

    private void SetupHttpContext()
    {
        var userId = Guid.NewGuid().ToString();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    private static (AcademicPeriod period, Department dept, AcademicProgram prog, Section section) SeedRequiredData(Data.ApplicationDbContext ctx)
    {
        var dept = new Department { Name = "COLLEGE", Code = "COL", Description = "College Department" };
        ctx.Departments.Add(dept);

        var period = new AcademicPeriod { Name = "SY 2025-2026 2nd Sem", StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(4), IsCurrent = true };
        ctx.AcademicPeriods.Add(period);

        var prog = new AcademicProgram { Name = "BSIT", Code = "BSIT", DepartmentId = dept.Id };
        ctx.AcademicPrograms.Add(prog);

        var section = new Section { Name = "BSIT-3A", YearLevel = 3, AcademicProgramId = prog.Id, AcademicPeriodId = period.Id };
        ctx.Sections.Add(section);

        ctx.SaveChanges();
        return (period, dept, prog, section);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsStudent()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        var result = await svc.CreateAsync(new CreateStudentRequest
        {
            StudentIdNumber = "STU-001",
            FirstName = "John",
            LastName = "Doe",
            SectionId = section.Id
        });

        Assert.Equal("STU-001", result.StudentIdNumber);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal(section.Id, result.SectionId);
        Assert.Equal("BSIT-3A", result.SectionName);
        _auditLogMock.Verify(x => x.LogAsync("Create", "Student", It.IsAny<string>(), null, It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateStudentId_ThrowsInvalidOperation()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-001", FirstName = "John", LastName = "Doe", SectionId = section.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-001", FirstName = "Jane", LastName = "Doe", SectionId = section.Id }));
    }

    [Fact]
    public async Task CreateAsync_InvalidSection_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-001", FirstName = "John", LastName = "Doe", SectionId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStudent_ReturnsStudent()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-002", FirstName = "Jane", LastName = "Smith", SectionId = section.Id });
        var result = await svc.GetByIdAsync(created.Id);

        Assert.Equal("STU-002", result.StudentIdNumber);
        Assert.Equal("Jane", result.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesAndAudits()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-003", FirstName = "Alice", LastName = "Brown", SectionId = section.Id });
        var updated = await svc.UpdateAsync(created.Id, new UpdateStudentRequest { FirstName = "Alicia" });

        Assert.Equal("Alicia", updated.FirstName);
        Assert.Equal("Brown", updated.LastName);
        _auditLogMock.Verify(x => x.LogAsync("Update", "Student", created.Id.ToString(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ChangeEnrollmentStatus_Updates()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-004", FirstName = "Bob", LastName = "Jones", SectionId = section.Id });
        var updated = await svc.UpdateAsync(created.Id, new UpdateStudentRequest { EnrollmentStatus = EnrollmentStatus.Inactive });

        Assert.Equal("Inactive", updated.EnrollmentStatus);
    }

    [Fact]
    public async Task DeleteAsync_ExistingStudent_SoftDeletes()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-005", FirstName = "Charlie", LastName = "Wilson", SectionId = section.Id });
        await svc.DeleteAsync(created.Id);

        // Soft-deleted — not found via normal query
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(created.Id));
        _auditLogMock.Verify(x => x.LogAsync("Delete", "Student", created.Id.ToString(), It.IsAny<string>(), null, It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsKeyNotFound()
    {
        using var ctx = TestDbContextFactory.Create();
        SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        for (int i = 1; i <= 5; i++)
            await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = $"STU-{i:D3}", FirstName = $"Student{i}", LastName = "Test", SectionId = section.Id });

        var result = await svc.GetAllAsync(1, 3, null, null);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-100", FirstName = "Alpha", LastName = "Test", SectionId = section.Id });
        await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-101", FirstName = "Beta", LastName = "Test", SectionId = section.Id });

        var result = await svc.GetAllAsync(1, 10, null, "Alpha");

        Assert.Single(result.Items);
        Assert.Equal("Alpha", result.Items[0].FirstName);
    }

    [Fact]
    public async Task GetAllAsync_WithSectionFilter_FiltersResults()
    {
        using var ctx = TestDbContextFactory.Create();
        var (period, dept, prog, section) = SeedRequiredData(ctx);

        var section2 = new Section { Name = "BSIT-3B", YearLevel = 3, AcademicProgramId = prog.Id, AcademicPeriodId = period.Id };
        ctx.Sections.Add(section2);
        ctx.SaveChanges();

        var svc = CreateService(ctx);
        await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-A", FirstName = "A", LastName = "Test", SectionId = section.Id });
        await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-B", FirstName = "B", LastName = "Test", SectionId = section2.Id });

        var result = await svc.GetAllAsync(1, 10, section.Id, null);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task RegenerateQrCodeAsync_GeneratesNewQr()
    {
        using var ctx = TestDbContextFactory.Create();
        var (_, _, _, section) = SeedRequiredData(ctx);
        var svc = CreateService(ctx);

        var created = await svc.CreateAsync(new CreateStudentRequest { StudentIdNumber = "STU-QR", FirstName = "Qr", LastName = "Test", SectionId = section.Id });
        var regenerated = await svc.RegenerateQrCodeAsync(created.Id);

        Assert.NotEqual(created.QrCodeData, regenerated.QrCodeData);
        Assert.StartsWith("STU-QR-", regenerated.QrCodeData);
    }
}
