using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for managing physical classrooms: rooms where classes are held
public class ClassroomsService : IClassroomsService
{
    private readonly AppDbContext _context;

    public ClassroomsService(AppDbContext context)
    {
        _context = context;
    }

    // Retrieves all classrooms ordered by name
    public async Task<List<ClassroomDto>> GetAllClassroomsAsync()
    {
        var classrooms = await _context.Classrooms
            .OrderBy(c => c.Name)
            .Select(c => new ClassroomDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return classrooms;
    }

    // Retrieves a single classroom by ID
    public async Task<ClassroomDto> GetClassroomByIdAsync(int id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);

        if (classroom == null)
        {
            throw new KeyNotFoundException("Classroom not found.");
        }

        var dto = new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            CreatedAt = classroom.CreatedAt
        };

        return dto;
    }

    // Creates a new classroom
    public async Task<ClassroomDto> CreateClassroomAsync(CreateClassroomRequest request)
    {
        var classroom = new Classroom
        {
            Name = request.Name,
            Description = request.Description
        };

        _context.Classrooms.Add(classroom);
        await _context.SaveChangesAsync();

        var dto = new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            CreatedAt = classroom.CreatedAt
        };

        return dto;
    }

    // Updates an existing classroom
    public async Task<ClassroomDto> UpdateClassroomAsync(int id, UpdateClassroomRequest request)
    {
        var classroom = await _context.Classrooms.FindAsync(id);

        if (classroom == null)
        {
            throw new KeyNotFoundException("Classroom not found.");
        }

        if (!string.IsNullOrEmpty(request.Name))
            classroom.Name = request.Name;
        if (request.Description != null)
            classroom.Description = request.Description;

        await _context.SaveChangesAsync();

        var dto = new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            CreatedAt = classroom.CreatedAt
        };

        return dto;
    }

    // Deletes a classroom if not assigned to any sections
    public async Task DeleteClassroomAsync(int id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);

        if (classroom == null)
        {
            throw new KeyNotFoundException("Classroom not found.");
        }

        // Prevent deletion if classroom is in use by sections
        var isInUse = await _context.Sections.AnyAsync(s => s.ClassroomId == id);
        if (isInUse)
        {
            throw new InvalidOperationException("Cannot delete classroom that is assigned to sections.");
        }

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();
    }
}