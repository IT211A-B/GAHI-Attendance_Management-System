using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for managing academic years: creation, activation, and date validation
public class AcademicYearsService : IAcademicYearsService
{
    private readonly AppDbContext _context;

    public AcademicYearsService(AppDbContext context)
    {
        _context = context;
    }

    // Retrieves all academic years ordered by start date (newest first)
    public async Task<ApiResponse<List<AcademicYearDto>>> GetAllAcademicYearsAsync()
    {
        var academicYears = await _context.AcademicYears
            .OrderByDescending(ay => ay.StartDate)
            .Select(ay => new AcademicYearDto
            {
                Id = ay.Id,
                YearLabel = ay.YearLabel,
                StartDate = ay.StartDate,
                EndDate = ay.EndDate,
                IsActive = ay.IsActive,
                CreatedAt = ay.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<AcademicYearDto>>.SuccessResponse(academicYears);
    }

    // Retrieves a single academic year by ID
    public async Task<ApiResponse<AcademicYearDto>> GetAcademicYearByIdAsync(int id)
    {
        var academicYear = await _context.AcademicYears.FindAsync(id);

        if (academicYear == null)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("NOT_FOUND", "Academic year not found.");
        }

        var dto = new AcademicYearDto
        {
            Id = academicYear.Id,
            YearLabel = academicYear.YearLabel,
            StartDate = academicYear.StartDate,
            EndDate = academicYear.EndDate,
            IsActive = academicYear.IsActive,
            CreatedAt = academicYear.CreatedAt
        };

        return ApiResponse<AcademicYearDto>.SuccessResponse(dto);
    }

    // Creates a new academic year with date validation and overlap checking
    public async Task<ApiResponse<AcademicYearDto>> CreateAcademicYearAsync(CreateAcademicYearRequest request)
    {
        // Validate that end date is after start date
        if (request.EndDate <= request.StartDate)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("VALIDATION_ERROR", "End date must be after start date.");
        }

        // Check for overlapping academic years
        var hasOverlap = await _context.AcademicYears
            .AnyAsync(ay => ay.StartDate <= request.EndDate && ay.EndDate >= request.StartDate);

        if (hasOverlap)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("VALIDATION_ERROR", "Academic year dates overlap with an existing academic year.");
        }

        var academicYear = new AcademicYear
        {
            YearLabel = request.YearLabel,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = false
        };

        _context.AcademicYears.Add(academicYear);
        await _context.SaveChangesAsync();

        var dto = new AcademicYearDto
        {
            Id = academicYear.Id,
            YearLabel = academicYear.YearLabel,
            StartDate = academicYear.StartDate,
            EndDate = academicYear.EndDate,
            IsActive = academicYear.IsActive,
            CreatedAt = academicYear.CreatedAt
        };

        return ApiResponse<AcademicYearDto>.SuccessResponse(dto);
    }

    // Updates an existing academic year with date validation
    public async Task<ApiResponse<AcademicYearDto>> UpdateAcademicYearAsync(int id, UpdateAcademicYearRequest request)
    {
        var academicYear = await _context.AcademicYears.FindAsync(id);

        if (academicYear == null)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("NOT_FOUND", "Academic year not found.");
        }

        var startDate = request.StartDate ?? academicYear.StartDate;
        var endDate = request.EndDate ?? academicYear.EndDate;

        // Validate that end date is after start date
        if (endDate <= startDate)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("VALIDATION_ERROR", "End date must be after start date.");
        }

        // Check for overlapping academic years (excluding current)
        var hasOverlap = await _context.AcademicYears
            .AnyAsync(ay => ay.Id != id && ay.StartDate <= endDate && ay.EndDate >= startDate);

        if (hasOverlap)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("VALIDATION_ERROR", "Academic year dates overlap with an existing academic year.");
        }

        if (!string.IsNullOrEmpty(request.YearLabel))
            academicYear.YearLabel = request.YearLabel;
        if (request.StartDate.HasValue)
            academicYear.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue)
            academicYear.EndDate = request.EndDate.Value;

        await _context.SaveChangesAsync();

        var dto = new AcademicYearDto
        {
            Id = academicYear.Id,
            YearLabel = academicYear.YearLabel,
            StartDate = academicYear.StartDate,
            EndDate = academicYear.EndDate,
            IsActive = academicYear.IsActive,
            CreatedAt = academicYear.CreatedAt
        };

        return ApiResponse<AcademicYearDto>.SuccessResponse(dto);
    }

    // Deletes an academic year if not in use by sections or enrollments
    public async Task<ApiResponse<bool>> DeleteAcademicYearAsync(int id)
    {
        var academicYear = await _context.AcademicYears.FindAsync(id);

        if (academicYear == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Academic year not found.");
        }

        // Prevent deletion if academic year has sections assigned
        var isInUse = await _context.Sections.AnyAsync(s => s.AcademicYearId == id);
        if (isInUse)
        {
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete academic year that has sections assigned.");
        }

        // Prevent deletion if academic year has enrollments
        var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.AcademicYearId == id);
        if (hasEnrollments)
        {
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete academic year that has enrollments.");
        }

        _context.AcademicYears.Remove(academicYear);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Activates an academic year and deactivates all others (only one can be active)
    public async Task<ApiResponse<AcademicYearDto>> ActivateAcademicYearAsync(int id)
    {
        var academicYear = await _context.AcademicYears.FindAsync(id);

        if (academicYear == null)
        {
            return ApiResponse<AcademicYearDto>.ErrorResponse("NOT_FOUND", "Academic year not found.");
        }

        // Deactivate all other academic years first (only one active at a time)
        var allAcademicYears = await _context.AcademicYears.ToListAsync();
        foreach (var ay in allAcademicYears)
        {
            ay.IsActive = (ay.Id == id);
        }

        await _context.SaveChangesAsync();

        var dto = new AcademicYearDto
        {
            Id = academicYear.Id,
            YearLabel = academicYear.YearLabel,
            StartDate = academicYear.StartDate,
            EndDate = academicYear.EndDate,
            IsActive = true,
            CreatedAt = academicYear.CreatedAt
        };

        return ApiResponse<AcademicYearDto>.SuccessResponse(dto);
    }
}