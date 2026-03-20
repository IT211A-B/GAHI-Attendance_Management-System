using Microsoft.EntityFrameworkCore;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Models;

namespace Donbosco_Attendance_Management_System.Services;

public interface ISectionsService
{
    Task<ListResponse<SectionResponse>> GetAllSectionsAsync();
    Task<SectionResponse?> GetSectionByIdAsync(Guid id);
    Task<(SectionResponse? Section, string? ErrorCode, string? ErrorMessage)> CreateSectionAsync(CreateSectionRequest request);
    Task<(SectionResponse? Section, string? ErrorCode, string? ErrorMessage)> UpdateSectionAsync(Guid id, UpdateSectionRequest request);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteSectionAsync(Guid id);
}

public class SectionsService : ISectionsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SectionsService> _logger;

    public SectionsService(AppDbContext context, ILogger<SectionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ListResponse<SectionResponse>> GetAllSectionsAsync()
    {
        var sections = await _context.Sections
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return new ListResponse<SectionResponse>
        {
            Items = sections.Select(MapToSectionResponse).ToList(),
            Total = sections.Count
        };
    }

    public async Task<SectionResponse?> GetSectionByIdAsync(Guid id)
    {
        var section = await _context.Sections.FindAsync(id);
        return section != null ? MapToSectionResponse(section) : null;
    }

    public async Task<(SectionResponse? Section, string? ErrorCode, string? ErrorMessage)> CreateSectionAsync(CreateSectionRequest request)
    {
        // Check for duplicate section name
        var existingSection = await _context.Sections
            .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Name.ToLower());

        if (existingSection != null)
        {
            return (null, ErrorCodes.VALIDATION_ERROR, "A section with this name already exists");
        }

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Section created: {Name}", section.Name);
        return (MapToSectionResponse(section), null, null);
    }

    public async Task<(SectionResponse? Section, string? ErrorCode, string? ErrorMessage)> UpdateSectionAsync(Guid id, UpdateSectionRequest request)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return (null, ErrorCodes.NOT_FOUND, "Section not found");
        }

        // Check for duplicate name if name is being changed
        if (!string.IsNullOrEmpty(request.Name) && request.Name.ToLower() != section.Name.ToLower())
        {
            var existingSection = await _context.Sections
                .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Name.ToLower() && s.Id != id);

            if (existingSection != null)
            {
                return (null, ErrorCodes.VALIDATION_ERROR, "A section with this name already exists");
            }
            section.Name = request.Name.Trim();
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Section updated: {Name}", section.Name);
        return (MapToSectionResponse(section), null, null);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteSectionAsync(Guid id)
    {
        var section = await _context.Sections.FindAsync(id);
        if (section == null)
        {
            return (false, ErrorCodes.NOT_FOUND, "Section not found");
        }

        // Check if section has students
        var hasStudents = await _context.Students.AnyAsync(s => s.SectionId == id);
        if (hasStudents)
        {
            return (false, ErrorCodes.VALIDATION_ERROR, "Cannot delete section with students. Remove students first.");
        }

        // Check if section has active schedules
        var hasSchedules = await _context.Schedules.AnyAsync(s => s.SectionId == id);
        if (hasSchedules)
        {
            return (false, ErrorCodes.CONFLICT_SECTION_SLOT, "Cannot delete section with active schedules. Remove schedules first.");
        }

        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Section deleted: {Name}", section.Name);
        return (true, null, null);
    }

    private static SectionResponse MapToSectionResponse(Section section)
    {
        return new SectionResponse
        {
            Id = section.Id,
            Name = section.Name,
            CreatedAt = section.CreatedAt
        };
    }
}