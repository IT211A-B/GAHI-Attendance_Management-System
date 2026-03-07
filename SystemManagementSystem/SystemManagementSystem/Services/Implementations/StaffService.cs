using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Staff;
using SystemManagementSystem.Services.Interfaces;
using StaffEntity = SystemManagementSystem.Models.Entities.Staff;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Services.Implementations;

public class StaffService : IStaffService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StaffService(ApplicationDbContext context, IAuditLogService auditLog, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _auditLog = auditLog;
        _httpContextAccessor = httpContextAccessor;
    }

    private Guid? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : null;
    }

    public async Task<PagedResult<StaffResponse>> GetAllAsync(int page, int pageSize, Guid? departmentId, string? search)
    {
        var query = _context.Staff
            .Include(s => s.Department)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(s => s.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.EmployeeIdNumber.ToLower().Contains(term));
        }

        query = query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<StaffResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StaffResponse> GetByIdAsync(Guid id)
    {
        var staff = await _context.Staff
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Staff with ID {id} not found.");

        return MapToResponse(staff);
    }

    public async Task<StaffResponse> CreateAsync(CreateStaffRequest request)
    {
        if (await _context.Staff.AnyAsync(s => s.EmployeeIdNumber == request.EmployeeIdNumber))
            throw new InvalidOperationException($"Employee ID '{request.EmployeeIdNumber}' already exists.");

        if (!await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            throw new KeyNotFoundException($"Department with ID {request.DepartmentId} not found.");

        var staff = new StaffEntity
        {
            EmployeeIdNumber = request.EmployeeIdNumber,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            Email = request.Email,
            ContactNumber = request.ContactNumber,
            StaffType = request.StaffType,
            DepartmentId = request.DepartmentId,
            QrCodeData = request.EmployeeIdNumber // Default QR = employee ID
        };

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync("Create", "Staff", staff.Id.ToString(),
            null,
            JsonSerializer.Serialize(new { staff.EmployeeIdNumber, staff.FirstName, staff.LastName, staff.Email, StaffType = staff.StaffType.ToString(), staff.DepartmentId }),
            GetCurrentUserId());

        return await GetByIdAsync(staff.Id);
    }

    public async Task<StaffResponse> UpdateAsync(Guid id, UpdateStaffRequest request)
    {
        var staff = await _context.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff with ID {id} not found.");

        var oldValues = JsonSerializer.Serialize(new { staff.FirstName, staff.LastName, staff.Email, StaffType = staff.StaffType.ToString(), staff.DepartmentId });

        if (request.FirstName != null) staff.FirstName = request.FirstName;
        if (request.MiddleName != null) staff.MiddleName = request.MiddleName;
        if (request.LastName != null) staff.LastName = request.LastName;
        if (request.Email != null) staff.Email = request.Email;
        if (request.ContactNumber != null) staff.ContactNumber = request.ContactNumber;
        if (request.StaffType.HasValue) staff.StaffType = request.StaffType.Value;

        if (request.DepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
                throw new KeyNotFoundException($"Department with ID {request.DepartmentId} not found.");
            staff.DepartmentId = request.DepartmentId.Value;
        }

        await _context.SaveChangesAsync();

        var newValues = JsonSerializer.Serialize(new { staff.FirstName, staff.LastName, staff.Email, StaffType = staff.StaffType.ToString(), staff.DepartmentId });
        await _auditLog.LogAsync("Update", "Staff", id.ToString(),
            JsonSerializer.Serialize(new { staff.FirstName, staff.LastName, staff.Email, StaffType = staff.StaffType.ToString(), staff.DepartmentId }),
            newValues, GetCurrentUserId());

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var staff = await _context.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff with ID {id} not found.");

        var oldValues = JsonSerializer.Serialize(new { staff.EmployeeIdNumber, staff.FirstName, staff.LastName, staff.Email });

        staff.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync("Delete", "Staff", id.ToString(), oldValues, null, GetCurrentUserId());
    }

    public async Task<StaffResponse> RegenerateQrCodeAsync(Guid id)
    {
        var staff = await _context.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff with ID {id} not found.");

        staff.QrCodeData = $"{staff.EmployeeIdNumber}-{Guid.NewGuid().ToString("N")[..8]}";
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private static StaffResponse MapToResponse(StaffEntity s) => new()
    {
        Id = s.Id,
        EmployeeIdNumber = s.EmployeeIdNumber,
        FirstName = s.FirstName,
        MiddleName = s.MiddleName,
        LastName = s.LastName,
        Email = s.Email,
        ContactNumber = s.ContactNumber,
        QrCodeData = s.QrCodeData,
        StaffType = s.StaffType.ToString(),
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department.Name,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
