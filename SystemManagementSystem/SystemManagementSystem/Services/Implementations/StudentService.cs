using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Students;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;

    public StudentService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<StudentResponse>> GetAllAsync(int page, int pageSize, Guid? sectionId, string? search)
    {
        var query = _context.Students
            .Include(s => s.Section)
                .ThenInclude(sec => sec.AcademicProgram)
                    .ThenInclude(p => p.Department)
            .AsQueryable();

        if (sectionId.HasValue)
            query = query.Where(s => s.SectionId == sectionId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                s.StudentIdNumber.ToLower().Contains(term));
        }

        query = query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<StudentResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<StudentResponse> GetByIdAsync(Guid id)
    {
        var student = await GetStudentWithIncludes()
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        return MapToResponse(student);
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request)
    {
        if (await _context.Students.AnyAsync(s => s.StudentIdNumber == request.StudentIdNumber))
            throw new InvalidOperationException($"Student ID '{request.StudentIdNumber}' already exists.");

        if (!await _context.Sections.AnyAsync(s => s.Id == request.SectionId))
            throw new KeyNotFoundException($"Section with ID {request.SectionId} not found.");

        var student = new Student
        {
            StudentIdNumber = request.StudentIdNumber,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            Email = request.Email,
            ContactNumber = request.ContactNumber,
            SectionId = request.SectionId,
            QrCodeData = request.StudentIdNumber // Default QR = student ID number
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(student.Id);
    }

    public async Task<StudentResponse> UpdateAsync(Guid id, UpdateStudentRequest request)
    {
        var student = await _context.Students.FindAsync(id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        if (request.FirstName != null) student.FirstName = request.FirstName;
        if (request.MiddleName != null) student.MiddleName = request.MiddleName;
        if (request.LastName != null) student.LastName = request.LastName;
        if (request.Email != null) student.Email = request.Email;
        if (request.ContactNumber != null) student.ContactNumber = request.ContactNumber;
        if (request.EnrollmentStatus.HasValue) student.EnrollmentStatus = request.EnrollmentStatus.Value;

        if (request.SectionId.HasValue)
        {
            if (!await _context.Sections.AnyAsync(s => s.Id == request.SectionId.Value))
                throw new KeyNotFoundException($"Section with ID {request.SectionId} not found.");
            student.SectionId = request.SectionId.Value;
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var student = await _context.Students.FindAsync(id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        student.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task<StudentResponse> RegenerateQrCodeAsync(Guid id)
    {
        var student = await _context.Students.FindAsync(id)
            ?? throw new KeyNotFoundException($"Student with ID {id} not found.");

        student.QrCodeData = $"{student.StudentIdNumber}-{Guid.NewGuid().ToString("N")[..8]}";
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private IQueryable<Student> GetStudentWithIncludes() =>
        _context.Students
            .Include(s => s.Section)
                .ThenInclude(sec => sec.AcademicProgram)
                    .ThenInclude(p => p.Department);

    private static StudentResponse MapToResponse(Student s) => new()
    {
        Id = s.Id,
        StudentIdNumber = s.StudentIdNumber,
        FirstName = s.FirstName,
        MiddleName = s.MiddleName,
        LastName = s.LastName,
        Email = s.Email,
        ContactNumber = s.ContactNumber,
        QrCodeData = s.QrCodeData,
        EnrollmentStatus = s.EnrollmentStatus.ToString(),
        SectionId = s.SectionId,
        SectionName = s.Section.Name,
        AcademicProgramName = s.Section.AcademicProgram.Name,
        DepartmentName = s.Section.AcademicProgram.Department.Name,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
