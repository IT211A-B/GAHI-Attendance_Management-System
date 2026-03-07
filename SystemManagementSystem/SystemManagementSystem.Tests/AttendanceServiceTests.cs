using Moq;
using SystemManagementSystem.DTOs.Attendance;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Models.Enums;
using SystemManagementSystem.Services.Implementations;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Tests;

public class AttendanceServiceTests
{
    private static (Data.ApplicationDbContext ctx, Department dept, Section section, GateTerminal terminal) SeedFullData()
    {
        var ctx = TestDbContextFactory.Create();

        var dept = new Department { Name = "COLLEGE", Code = "COL" };
        ctx.Departments.Add(dept);

        var period = new AcademicPeriod { Name = "SY 2025-2026", StartDate = DateTime.UtcNow.AddMonths(-2), EndDate = DateTime.UtcNow.AddMonths(4), IsCurrent = true };
        ctx.AcademicPeriods.Add(period);

        var prog = new AcademicProgram { Name = "BSIT", Code = "BSIT", DepartmentId = dept.Id };
        ctx.AcademicPrograms.Add(prog);

        var section = new Section { Name = "BSIT-3A", YearLevel = 3, AcademicProgramId = prog.Id, AcademicPeriodId = period.Id };
        ctx.Sections.Add(section);

        var terminal = new GateTerminal { Name = "Main Gate", Location = "Front", TerminalType = TerminalType.QRScanner, IsActive = true };
        ctx.GateTerminals.Add(terminal);

        // Seed business rules
        ctx.BusinessRules.Add(new BusinessRule { RuleKey = "MORNING_CUTOFF_TIME", RuleValue = "08:00:00" });
        ctx.BusinessRules.Add(new BusinessRule { RuleKey = "AFTERNOON_CUTOFF_TIME", RuleValue = "13:00:00" });
        ctx.BusinessRules.Add(new BusinessRule { RuleKey = "GRACE_PERIOD_MINUTES", RuleValue = "15" });

        ctx.SaveChanges();
        return (ctx, dept, section, terminal);
    }

    [Fact]
    public async Task ProcessScanAsync_StudentQrCode_CreatesAttendanceLog()
    {
        var (ctx, dept, section, terminal) = SeedFullData();

        var student = new Student { StudentIdNumber = "STU-001", FirstName = "John", LastName = "Doe", QrCodeData = "STU-001", SectionId = section.Id };
        ctx.Students.Add(student);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        var result = await svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "STU-001" });

        Assert.Equal("John Doe", result.PersonName);
        Assert.Equal("Student", result.PersonType);
        Assert.Equal("STU-001", result.IdNumber);
        Assert.Equal("Entry", result.ScanType);
        Assert.Equal("Verified", result.VerificationStatus);
        Assert.Equal("Main Gate", result.TerminalName);

        ctx.Dispose();
    }

    [Fact]
    public async Task ProcessScanAsync_StaffQrCode_CreatesAttendanceLog()
    {
        var (ctx, dept, section, terminal) = SeedFullData();

        var staff = new Models.Entities.Staff { EmployeeIdNumber = "EMP-001", FirstName = "Jane", LastName = "Smith", QrCodeData = "EMP-001", StaffType = StaffType.Teaching, DepartmentId = dept.Id };
        ctx.Staff.Add(staff);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        var result = await svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "EMP-001" });

        Assert.Equal("Jane Smith", result.PersonName);
        Assert.Equal("Staff", result.PersonType);
        Assert.Equal("Entry", result.ScanType);

        ctx.Dispose();
    }

    [Fact]
    public async Task ProcessScanAsync_SecondScanSameDay_ReturnsExit()
    {
        var (ctx, dept, section, terminal) = SeedFullData();

        var student = new Student { StudentIdNumber = "STU-002", FirstName = "Bob", LastName = "Jones", QrCodeData = "STU-002", SectionId = section.Id };
        ctx.Students.Add(student);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        var firstScan = await svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "STU-002" });
        Assert.Equal("Entry", firstScan.ScanType);

        var secondScan = await svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "STU-002" });
        Assert.Equal("Exit", secondScan.ScanType);

        ctx.Dispose();
    }

    [Fact]
    public async Task ProcessScanAsync_UnknownQrCode_ThrowsKeyNotFound()
    {
        var (ctx, _, _, terminal) = SeedFullData();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "UNKNOWN-QR" }));

        ctx.Dispose();
    }

    [Fact]
    public async Task ProcessScanAsync_InactiveTerminal_ThrowsKeyNotFound()
    {
        var (ctx, _, _, _) = SeedFullData();

        // Create an inactive terminal
        var inactiveTerminal = new GateTerminal { Name = "Inactive", Location = "Back", IsActive = false };
        ctx.GateTerminals.Add(inactiveTerminal);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.ProcessScanAsync(new ScanRequest { GateTerminalId = inactiveTerminal.Id, RawScanData = "ANY" }));

        ctx.Dispose();
    }

    [Fact]
    public async Task ProcessScanAsync_InactiveStudent_ThrowsInvalidOperation()
    {
        var (ctx, dept, section, terminal) = SeedFullData();

        var student = new Student { StudentIdNumber = "STU-INACTIVE", FirstName = "Drop", LastName = "Out", QrCodeData = "STU-INACTIVE", SectionId = section.Id, EnrollmentStatus = EnrollmentStatus.Inactive };
        ctx.Students.Add(student);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "STU-INACTIVE" }));

        ctx.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingLog_ReturnsLog()
    {
        var (ctx, dept, section, terminal) = SeedFullData();

        var student = new Student { StudentIdNumber = "STU-LU", FirstName = "Look", LastName = "Up", QrCodeData = "STU-LU", SectionId = section.Id };
        ctx.Students.Add(student);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        var scan = await svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "STU-LU" });
        var result = await svc.GetByIdAsync(scan.AttendanceLogId);

        Assert.Equal(scan.AttendanceLogId, result.Id);
        Assert.Equal("STU-LU", result.StudentIdNumber);

        ctx.Dispose();
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsFilteredResults()
    {
        var (ctx, dept, section, terminal) = SeedFullData();

        var student = new Student { StudentIdNumber = "STU-FL", FirstName = "Filter", LastName = "Test", QrCodeData = "STU-FL", SectionId = section.Id };
        ctx.Students.Add(student);
        ctx.SaveChanges();

        var businessRuleSvc = new BusinessRuleService(ctx);
        var svc = new AttendanceService(ctx, businessRuleSvc);

        await svc.ProcessScanAsync(new ScanRequest { GateTerminalId = terminal.Id, RawScanData = "STU-FL" });

        var logs = await svc.GetLogsAsync(new AttendanceFilterRequest { Date = DateTime.UtcNow.Date, Page = 1, PageSize = 10 });

        Assert.NotEmpty(logs.Items);

        ctx.Dispose();
    }
}
