using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.AcademicPrograms;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class AcademicProgramService : IAcademicProgramService
{
    private readonly ApplicationDbContext _context;

    public AcademicProgramService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<AcademicProgramResponse>> GetAllAsync(int page, int pageSize, Guid? departmentId)
    {
        var query = _context.AcademicPrograms
            .Include(p => p.Department)
            .Include(p => p.Sections)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(p => p.DepartmentId == departmentId.Value);

        query = query.OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AcademicProgramResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AcademicProgramResponse> GetByIdAsync(Guid id)
    {
        var program = await _context.AcademicPrograms
            .Include(p => p.Department)
            .Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Academic program with ID {id} not found.");

        return MapToResponse(program);
    }

    public async Task<AcademicProgramResponse> CreateAsync(CreateAcademicProgramRequest request)
    {
        if (await _context.AcademicPrograms.AnyAsync(p => p.Code == request.Code))
            throw new InvalidOperationException($"Program code '{request.Code}' already exists.");

        if (!await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            throw new KeyNotFoundException($"Department with ID {request.DepartmentId} not found.");

        var program = new AcademicProgram
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            DepartmentId = request.DepartmentId
        };

        _context.AcademicPrograms.Add(program);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(program.Id);
    }

    public async Task<AcademicProgramResponse> UpdateAsync(Guid id, UpdateAcademicProgramRequest request)
    {
        var program = await _context.AcademicPrograms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Academic program with ID {id} not found.");

        if (request.Code != null && request.Code != program.Code)
        {
            if (await _context.AcademicPrograms.AnyAsync(p => p.Code == request.Code && p.Id != id))
                throw new InvalidOperationException($"Program code '{request.Code}' already exists.");
            program.Code = request.Code;
        }

        if (request.Name != null) program.Name = request.Name;
        if (request.Description != null) program.Description = request.Description;
        if (request.DepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
                throw new KeyNotFoundException($"Department with ID {request.DepartmentId} not found.");
            program.DepartmentId = request.DepartmentId.Value;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var program = await _context.AcademicPrograms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Academic program with ID {id} not found.");

        program.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private static AcademicProgramResponse MapToResponse(AcademicProgram p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Code = p.Code,
        Description = p.Description,
        DepartmentId = p.DepartmentId,
        DepartmentName = p.Department.Name,
        SectionCount = p.Sections.Count,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
