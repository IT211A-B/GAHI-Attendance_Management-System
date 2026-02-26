using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.AcademicPeriods;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class AcademicPeriodService : IAcademicPeriodService
{
    private readonly ApplicationDbContext _context;

    public AcademicPeriodService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<AcademicPeriodResponse>> GetAllAsync(int page, int pageSize)
    {
        var query = _context.AcademicPeriods
            .Include(p => p.Sections)
            .OrderByDescending(p => p.StartDate);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AcademicPeriodResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AcademicPeriodResponse> GetByIdAsync(Guid id)
    {
        var period = await _context.AcademicPeriods
            .Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Academic period with ID {id} not found.");

        return MapToResponse(period);
    }

    public async Task<AcademicPeriodResponse> GetCurrentAsync()
    {
        var period = await _context.AcademicPeriods
            .Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.IsCurrent)
            ?? throw new KeyNotFoundException("No current academic period is set.");

        return MapToResponse(period);
    }

    public async Task<AcademicPeriodResponse> CreateAsync(CreateAcademicPeriodRequest request)
    {
        if (await _context.AcademicPeriods.AnyAsync(p => p.Name == request.Name))
            throw new InvalidOperationException($"Academic period '{request.Name}' already exists.");

        if (request.StartDate >= request.EndDate)
            throw new ArgumentException("Start date must be before end date.");

        if (request.IsCurrent)
        {
            var current = await _context.AcademicPeriods.Where(p => p.IsCurrent).ToListAsync();
            current.ForEach(p => p.IsCurrent = false);
        }

        var period = new AcademicPeriod
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent
        };

        _context.AcademicPeriods.Add(period);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(period.Id);
    }

    public async Task<AcademicPeriodResponse> UpdateAsync(Guid id, UpdateAcademicPeriodRequest request)
    {
        var period = await _context.AcademicPeriods.FindAsync(id)
            ?? throw new KeyNotFoundException($"Academic period with ID {id} not found.");

        if (request.Name != null)
        {
            if (await _context.AcademicPeriods.AnyAsync(p => p.Name == request.Name && p.Id != id))
                throw new InvalidOperationException($"Academic period '{request.Name}' already exists.");
            period.Name = request.Name;
        }

        if (request.StartDate.HasValue) period.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) period.EndDate = request.EndDate.Value;

        if (period.StartDate >= period.EndDate)
            throw new ArgumentException("Start date must be before end date.");

        if (request.IsCurrent == true)
        {
            var current = await _context.AcademicPeriods.Where(p => p.IsCurrent && p.Id != id).ToListAsync();
            current.ForEach(p => p.IsCurrent = false);
            period.IsCurrent = true;
        }
        else if (request.IsCurrent == false)
        {
            period.IsCurrent = false;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<AcademicPeriodResponse> SetCurrentAsync(Guid id)
    {
        var period = await _context.AcademicPeriods.FindAsync(id)
            ?? throw new KeyNotFoundException($"Academic period with ID {id} not found.");

        var current = await _context.AcademicPeriods.Where(p => p.IsCurrent).ToListAsync();
        current.ForEach(p => p.IsCurrent = false);
        period.IsCurrent = true;

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var period = await _context.AcademicPeriods.FindAsync(id)
            ?? throw new KeyNotFoundException($"Academic period with ID {id} not found.");

        period.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    private static AcademicPeriodResponse MapToResponse(AcademicPeriod p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsCurrent = p.IsCurrent,
        SectionCount = p.Sections.Count,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
