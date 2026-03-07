using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Departments;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DepartmentService(ApplicationDbContext context, IAuditLogService auditLog, IHttpContextAccessor httpContextAccessor)
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

    public async Task<PagedResult<DepartmentResponse>> GetAllAsync(int page, int pageSize)
    {
        var query = _context.Departments
            .Include(d => d.AcademicPrograms)
            .Include(d => d.Staff)
            .OrderBy(d => d.Name);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DepartmentResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<DepartmentResponse> GetByIdAsync(Guid id)
    {
        var dept = await _context.Departments
            .Include(d => d.AcademicPrograms)
            .Include(d => d.Staff)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Department with ID {id} not found.");

        return MapToResponse(dept);
    }

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request)
    {
        if (await _context.Departments.AnyAsync(d => d.Code == request.Code))
            throw new InvalidOperationException($"Department code '{request.Code}' already exists.");

        var dept = new Department
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync("Create", "Department", dept.Id.ToString(),
            null,
            JsonSerializer.Serialize(new { dept.Name, dept.Code, dept.Description }),
            GetCurrentUserId());

        return await GetByIdAsync(dept.Id);
    }

    public async Task<DepartmentResponse> UpdateAsync(Guid id, UpdateDepartmentRequest request)
    {
        var dept = await _context.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Department with ID {id} not found.");

        if (request.Code != null && request.Code != dept.Code)
        {
            if (await _context.Departments.AnyAsync(d => d.Code == request.Code && d.Id != id))
                throw new InvalidOperationException($"Department code '{request.Code}' already exists.");
            dept.Code = request.Code;
        }

        if (request.Name != null) dept.Name = request.Name;
        if (request.Description != null) dept.Description = request.Description;

        await _context.SaveChangesAsync();

        var newValues = JsonSerializer.Serialize(new { dept.Name, dept.Code, dept.Description });
        await _auditLog.LogAsync("Update", "Department", id.ToString(),
            JsonSerializer.Serialize(new { Name = dept.Name, Code = dept.Code, Description = dept.Description }),
            newValues, GetCurrentUserId());

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dept = await _context.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Department with ID {id} not found.");

        var oldValues = JsonSerializer.Serialize(new { dept.Name, dept.Code, dept.Description });

        dept.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync("Delete", "Department", id.ToString(), oldValues, null, GetCurrentUserId());
    }

    private static DepartmentResponse MapToResponse(Department d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Code = d.Code,
        Description = d.Description,
        ProgramCount = d.AcademicPrograms.Count,
        StaffCount = d.Staff.Count,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt
    };
}
