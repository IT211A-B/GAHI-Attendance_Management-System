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

    public DepartmentService(ApplicationDbContext context) => _context = context;

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
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dept = await _context.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Department with ID {id} not found.");

        dept.IsDeleted = true;
        await _context.SaveChangesAsync();
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
