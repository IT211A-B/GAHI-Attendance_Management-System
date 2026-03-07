using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Attendance;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Models.Enums;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

/// <summary>
/// Core attendance processing service. Handles QR scan resolution, entry/exit determination,
/// and late/on-time calculation based on configurable business rules.
/// </summary>
public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IBusinessRuleService _businessRuleService;

    public AttendanceService(ApplicationDbContext context, IBusinessRuleService businessRuleService)
    {
        _context = context;
        _businessRuleService = businessRuleService;
    }

    public async Task<ScanResponse> ProcessScanAsync(ScanRequest request)
    {
        // 1. Validate terminal
        var terminal = await _context.GateTerminals
            .FirstOrDefaultAsync(g => g.Id == request.GateTerminalId && g.IsActive)
            ?? throw new KeyNotFoundException("Gate terminal not found or is inactive.");

        // 2. Resolve QR code to person (student or staff)
        var person = await ResolvePersonAsync(request.RawScanData);

        // 3. Determine scan type (Entry/Exit) based on last scan today
        var scanType = await DetermineScanTypeAsync(person.PersonType, person.StudentId, person.StaffId);

        // 4. Calculate attendance status using business rules
        var status = await CalculateStatusAsync(scanType, person.DepartmentId);

        // 5. Create attendance log
        var log = new AttendanceLog
        {
            PersonType = person.PersonType,
            StudentId = person.StudentId,
            StaffId = person.StaffId,
            GateTerminalId = request.GateTerminalId,
            ScannedAt = DateTime.UtcNow,
            ScanType = scanType,
            Status = status,
            VerificationStatus = VerificationStatus.Verified,
            RawScanData = request.RawScanData,
            Remarks = request.Remarks
        };

        _context.AttendanceLogs.Add(log);
        await _context.SaveChangesAsync();

        return new ScanResponse
        {
            AttendanceLogId = log.Id,
            PersonName = person.PersonName,
            PersonType = person.PersonType.ToString(),
            IdNumber = person.IdNumber,
            ScanType = scanType.ToString(),
            Status = status.ToString(),
            VerificationStatus = log.VerificationStatus.ToString(),
            ScannedAt = log.ScannedAt,
            TerminalName = terminal.Name
        };
    }

    public async Task<PagedResult<AttendanceLogResponse>> GetLogsAsync(AttendanceFilterRequest filter)
    {
        var query = _context.AttendanceLogs
            .Include(a => a.Student)
            .Include(a => a.Staff)
            .Include(a => a.GateTerminal)
            .AsQueryable();

        // Date filters
        if (filter.Date.HasValue)
        {
            var date = filter.Date.Value.Date;
            query = query.Where(a => a.ScannedAt.Date == date);
        }
        else
        {
            if (filter.StartDate.HasValue)
                query = query.Where(a => a.ScannedAt >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(a => a.ScannedAt <= filter.EndDate.Value);
        }

        // Section filter (students only)
        if (filter.SectionId.HasValue)
            query = query.Where(a => a.Student != null && a.Student.SectionId == filter.SectionId.Value);

        // Status filter
        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<AttendanceStatus>(filter.Status, true, out var statusEnum))
            query = query.Where(a => a.Status == statusEnum);

        // Person type filter
        if (!string.IsNullOrEmpty(filter.PersonType) && Enum.TryParse<PersonType>(filter.PersonType, true, out var ptEnum))
            query = query.Where(a => a.PersonType == ptEnum);

        query = query.OrderByDescending(a => a.ScannedAt);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AttendanceLogResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AttendanceLogResponse> GetByIdAsync(Guid id)
    {
        var log = await _context.AttendanceLogs
            .Include(a => a.Student)
            .Include(a => a.Staff)
            .Include(a => a.GateTerminal)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException($"Attendance log with ID {id} not found.");

        return MapToResponse(log);
    }

    // ──── Private helpers ────

    private record ResolvedPerson(
        PersonType PersonType,
        Guid? StudentId,
        Guid? StaffId,
        string PersonName,
        string IdNumber,
        Guid? DepartmentId);

    private async Task<ResolvedPerson> ResolvePersonAsync(string rawScanData)
    {
        // Try student first (by QR code data or student ID number)
        var student = await _context.Students
            .Include(s => s.Section)
                .ThenInclude(sec => sec.AcademicProgram)
            .FirstOrDefaultAsync(s =>
                s.QrCodeData == rawScanData ||
                s.StudentIdNumber == rawScanData);

        if (student != null)
        {
            if (student.EnrollmentStatus != EnrollmentStatus.Active)
                throw new InvalidOperationException(
                    $"Student {student.StudentIdNumber} is not actively enrolled (status: {student.EnrollmentStatus}).");

            return new ResolvedPerson(
                PersonType.Student,
                student.Id,
                null,
                $"{student.FirstName} {student.LastName}",
                student.StudentIdNumber,
                student.Section.AcademicProgram.DepartmentId);
        }

        // Try staff (by QR code data or employee ID number)
        var staff = await _context.Staff
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s =>
                s.QrCodeData == rawScanData ||
                s.EmployeeIdNumber == rawScanData);

        if (staff != null)
        {
            return new ResolvedPerson(
                PersonType.Staff,
                null,
                staff.Id,
                $"{staff.FirstName} {staff.LastName}",
                staff.EmployeeIdNumber,
                staff.DepartmentId);
        }

        throw new KeyNotFoundException("No student or staff found matching the scanned QR code data.");
    }

    private async Task<ScanType> DetermineScanTypeAsync(PersonType personType, Guid? studentId, Guid? staffId)
    {
        var today = DateTime.UtcNow.Date;

        var lastScan = await _context.AttendanceLogs
            .Where(a => a.ScannedAt.Date == today)
            .Where(a =>
                (personType == PersonType.Student && a.StudentId == studentId) ||
                (personType == PersonType.Staff && a.StaffId == staffId))
            .OrderByDescending(a => a.ScannedAt)
            .FirstOrDefaultAsync();

        if (lastScan == null)
            return ScanType.Entry; // First scan of the day

        // Alternate between Entry and Exit
        return lastScan.ScanType == ScanType.Entry ? ScanType.Exit : ScanType.Entry;
    }

    private async Task<AttendanceStatus> CalculateStatusAsync(ScanType scanType, Guid? departmentId)
    {
        // Exit scans don't carry a meaningful "late/on-time" status
        if (scanType == ScanType.Exit)
            return AttendanceStatus.OnTime;

        var now = DateTime.UtcNow;
        var currentTime = now.TimeOfDay;

        // Load business rules
        var morningCutoffStr = await _businessRuleService.GetRuleValueAsync("MORNING_CUTOFF_TIME");
        var afternoonCutoffStr = await _businessRuleService.GetRuleValueAsync("AFTERNOON_CUTOFF_TIME");
        var graceStr = await _businessRuleService.GetRuleValueAsync("GRACE_PERIOD_MINUTES", departmentId);

        var morningCutoff = TimeSpan.TryParse(morningCutoffStr, out var mc) ? mc : new TimeSpan(8, 0, 0);
        var afternoonCutoff = TimeSpan.TryParse(afternoonCutoffStr, out var ac) ? ac : new TimeSpan(13, 0, 0);
        var gracePeriod = int.TryParse(graceStr, out var g) ? g : 0;

        // Pick the applicable cutoff based on time of day
        var applicableCutoff = currentTime < new TimeSpan(12, 0, 0) ? morningCutoff : afternoonCutoff;

        if (currentTime <= applicableCutoff)
            return AttendanceStatus.OnTime;

        if (currentTime <= applicableCutoff.Add(TimeSpan.FromMinutes(gracePeriod)))
            return AttendanceStatus.Late;

        return AttendanceStatus.Late;
    }

    private static AttendanceLogResponse MapToResponse(AttendanceLog a) => new()
    {
        Id = a.Id,
        PersonType = a.PersonType.ToString(),
        StudentId = a.StudentId,
        StudentName = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : null,
        StudentIdNumber = a.Student?.StudentIdNumber,
        StaffId = a.StaffId,
        StaffName = a.Staff != null ? $"{a.Staff.FirstName} {a.Staff.LastName}" : null,
        EmployeeIdNumber = a.Staff?.EmployeeIdNumber,
        GateTerminalId = a.GateTerminalId,
        GateTerminalName = a.GateTerminal.Name,
        ScannedAt = a.ScannedAt,
        ScanType = a.ScanType.ToString(),
        Status = a.Status.ToString(),
        VerificationStatus = a.VerificationStatus.ToString(),
        RawScanData = a.RawScanData,
        Remarks = a.Remarks,
        CreatedAt = a.CreatedAt
    };
}
