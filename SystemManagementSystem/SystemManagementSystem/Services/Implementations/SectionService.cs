using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Sections;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class SectionService : ISectionService
{
    private readonly ApplicationDbContext _context;

    public SectionService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<SectionResponse>> GetAllAsync(int page, int pageSize, Guid? programId, Guid? periodId)
    {
        var query = _context.Sections
            .Include(s => s.AcademicProgram)
            .Include(s => s.AcademicPeriod)
            .Include(s => s.Students)
            .AsQueryable();

        if (programId.HasValue) query = query.Where(s => s.AcademicProgramId == programId.Value);
        if (periodId.HasValue) query = query.Where(s => s.AcademicPeriodId == periodId.Value);

        query = query.OrderBy(s => s.AcademicProgram.Name).ThenBy(s => s.YearLevel).ThenBy(s => s.Name);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SectionResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SectionResponse> GetByIdAsync(Guid id)
    {
        var section = await _context.Sections
            .Include(s => s.AcademicProgram)
            .Include(s => s.AcademicPeriod)
            .Include(s => s.Students)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Section with ID {id} not found.");

        return MapToResponse(section);
    }

    public async Task<SectionResponse> CreateAsync(CreateSectionRequest request)
    {
        if (!await _context.AcademicPrograms.AnyAsync(p => p.Id == request.AcademicProgramId))
            throw new KeyNotFoundException($"Academic program with ID {request.AcademicProgramId} not found.");

        if (!await _context.AcademicPeriods.AnyAsync(p => p.Id == request.AcademicPeriodId))
            throw new KeyNotFoundException($"Academic period with ID {request.AcademicPeriodId} not found.");

        if (await _context.Sections.AnyAsync(s =>
            s.Name == request.Name &&
            s.AcademicProgramId == request.AcademicProgramId &&
            s.AcademicPeriodId == request.AcademicPeriodId))
            throw new InvalidOperationException("A section with this name already exists for the specified program and period.");

        var section = new Section
        {
            Name = request.Name,
            YearLevel = request.YearLevel,
            AcademicProgramId = request.AcademicProgramId,
            AcademicPeriodId = request.AcademicPeriodId
        };

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(section.Id);
    }

    public async Task<SectionResponse> UpdateAsync(Guid id, UpdateSectionRequest request)
    {
        var section = await _context.Sections.FindAsync(id)
            ?? throw new KeyNotFoundException($"Section with ID {id} not found.");

        if (request.Name != null) section.Name = request.Name;
        if (request.YearLevel.HasValue) section.YearLevel = request.YearLevel.Value;

        if (request.AcademicProgramId.HasValue)
        {
            if (!await _context.AcademicPrograms.AnyAsync(p => p.Id == request.AcademicProgramId.Value))
                throw new KeyNotFoundException($"Academic program with ID {request.AcademicProgramId} not found.");
            section.AcademicProgramId = request.AcademicProgramId.Value;
        }

        if (request.AcademicPeriodId.HasValue)
        {
            if (!await _context.AcademicPeriods.AnyAsync(p => p.Id == request.AcademicPeriodId.Value))
                throw new KeyNotFoundException($"Academic period with ID {request.AcademicPeriodId} not found.");
            section.AcademicPeriodId = request.AcademicPeriodId.Value;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var section = await _context.Sections.FindAsync(id)
            ?? throw new KeyNotFoundException($"Section with ID {id} not found.");

        section.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private static SectionResponse MapToResponse(Section s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        YearLevel = s.YearLevel,
        AcademicProgramId = s.AcademicProgramId,
        AcademicProgramName = s.AcademicProgram.Name,
        AcademicPeriodId = s.AcademicPeriodId,
        AcademicPeriodName = s.AcademicPeriod.Name,
        StudentCount = s.Students.Count,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
