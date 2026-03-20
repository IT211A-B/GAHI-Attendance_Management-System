using Microsoft.EntityFrameworkCore;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Models;

namespace Donbosco_Attendance_Management_System.Services;

public interface IClassroomsService
{
    Task<ListResponse<ClassroomResponse>> GetAllClassroomsAsync();
    Task<ClassroomResponse?> GetClassroomByIdAsync(Guid id);
    Task<(ClassroomResponse? Classroom, string? ErrorCode, string? ErrorMessage)> CreateClassroomAsync(CreateClassroomRequest request);
    Task<(ClassroomResponse? Classroom, string? ErrorCode, string? ErrorMessage)> UpdateClassroomAsync(Guid id, UpdateClassroomRequest request);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteClassroomAsync(Guid id);
}

public class ClassroomsService : IClassroomsService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClassroomsService> _logger;

    public ClassroomsService(AppDbContext context, ILogger<ClassroomsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ListResponse<ClassroomResponse>> GetAllClassroomsAsync()
    {
        var classrooms = await _context.Classrooms
            .AsNoTracking()
            .OrderBy(c => c.RoomNumber)
            .ToListAsync();

        return new ListResponse<ClassroomResponse>
        {
            Items = classrooms.Select(MapToClassroomResponse).ToList(),
            Total = classrooms.Count
        };
    }

    public async Task<ClassroomResponse?> GetClassroomByIdAsync(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        return classroom != null ? MapToClassroomResponse(classroom) : null;
    }

    public async Task<(ClassroomResponse? Classroom, string? ErrorCode, string? ErrorMessage)> CreateClassroomAsync(CreateClassroomRequest request)
    {
        // Check for duplicate room number
        var existingClassroom = await _context.Classrooms
            .FirstOrDefaultAsync(c => c.RoomNumber.ToLower() == request.RoomNumber.ToLower());

        if (existingClassroom != null)
        {
            return (null, ErrorCodes.VALIDATION_ERROR, "A classroom with this room number already exists");
        }

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            RoomNumber = request.RoomNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Classrooms.Add(classroom);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Classroom created: {RoomNumber} - {Name}", classroom.RoomNumber, classroom.Name);
        return (MapToClassroomResponse(classroom), null, null);
    }

    public async Task<(ClassroomResponse? Classroom, string? ErrorCode, string? ErrorMessage)> UpdateClassroomAsync(Guid id, UpdateClassroomRequest request)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
        {
            return (null, ErrorCodes.NOT_FOUND, "Classroom not found");
        }

        // Check for duplicate room number if room number is being changed
        if (!string.IsNullOrEmpty(request.RoomNumber) && request.RoomNumber.ToLower() != classroom.RoomNumber.ToLower())
        {
            var existingClassroom = await _context.Classrooms
                .FirstOrDefaultAsync(c => c.RoomNumber.ToLower() == request.RoomNumber.ToLower() && c.Id != id);

            if (existingClassroom != null)
            {
                return (null, ErrorCodes.VALIDATION_ERROR, "A classroom with this room number already exists");
            }
            classroom.RoomNumber = request.RoomNumber.Trim();
        }

        // Update name if provided
        if (!string.IsNullOrEmpty(request.Name))
        {
            classroom.Name = request.Name.Trim();
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Classroom updated: {RoomNumber}", classroom.RoomNumber);
        return (MapToClassroomResponse(classroom), null, null);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteClassroomAsync(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
        {
            return (false, ErrorCodes.NOT_FOUND, "Classroom not found");
        }

        // Check if classroom has active schedules
        var hasSchedules = await _context.Schedules.AnyAsync(s => s.ClassroomId == id);
        if (hasSchedules)
        {
            return (false, ErrorCodes.CONFLICT_CLASSROOM, "Cannot delete classroom with active schedules. Remove schedules first.");
        }

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Classroom deleted: {RoomNumber}", classroom.RoomNumber);
        return (true, null, null);
    }

    private static ClassroomResponse MapToClassroomResponse(Classroom classroom)
    {
        return new ClassroomResponse
        {
            Id = classroom.Id,
            Name = classroom.Name,
            RoomNumber = classroom.RoomNumber,
            CreatedAt = classroom.CreatedAt
        };
    }
}