using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for student profile operations with role-based access control
public class StudentsService : IStudentsService
{
    private readonly AppDbContext _context;

    // Inject database context through constructor
    public StudentsService(AppDbContext context)
    {
        _context = context;
    }

    // Retrieves full profile for the authenticated student based on their user ID
    public async Task<StudentProfileDto> GetMyProfileAsync(int userId)
    {
        var student = await _context.Students
            .Include(s => s.Course)
            .Include(s => s.Section)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            throw new KeyNotFoundException("Student profile not found. Only students can access this endpoint.");
        }

        var profile = MapToFullProfile(student);
        return profile;
    }

    // Returns appropriate profile based on requester's role
    public async Task<object> GetStudentProfileAsync(int studentId, int requesterUserId, string requesterRole)
    {
        // Fetch the student with related entities
        var student = await _context.Students
            .Include(s => s.Course)
            .Include(s => s.Section)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
        {
            throw new KeyNotFoundException("Student not found.");
        }

        // Admin: Return full profile for any student
        if (requesterRole == "admin")
        {
            var fullProfile = MapToFullProfile(student);
            return fullProfile;
        }

        // Student: Return full profile if viewing self, basic profile otherwise
        if (requesterRole == "student")
        {
            if (student.UserId == requesterUserId)
            {
                var selfProfile = MapToFullProfile(student);
                return selfProfile;
            }

            // Students can only view basic profiles of other students
            var basicProfile = MapToBasicProfile(student);
            return basicProfile;
        }

        // Teacher: Return basic profile only if student is in their section
        if (requesterRole == "teacher")
        {
            if (student.SectionId == null)
            {
                throw new UnauthorizedAccessException("You do not have access to this student's profile.");
            }

            // Check if teacher is assigned to the student's section
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == requesterUserId);
            if (teacher == null)
            {
                throw new KeyNotFoundException("Teacher profile not found.");
            }

            var isTeacherInSection = await _context.SectionTeachers
                .AnyAsync(st => st.SectionId == student.SectionId && st.TeacherId == teacher.Id);

            if (!isTeacherInSection)
            {
                throw new UnauthorizedAccessException("You do not have access to this student's profile.");
            }

            var teacherViewProfile = MapToBasicProfile(student);
            return teacherViewProfile;
        }

        throw new UnauthorizedAccessException("Access denied.");
    }

    // Returns list of basic profiles for students in a section
    public async Task<List<StudentBasicProfileDto>> GetStudentsBySectionAsync(
        int sectionId,
        int requesterUserId,
        string requesterRole)
    {
        // Verify section exists
        var sectionExists = await _context.Sections.AnyAsync(s => s.Id == sectionId);
        if (!sectionExists)
        {
            throw new KeyNotFoundException("Section not found.");
        }

        // Admin: Can view all students in any section
        if (requesterRole == "admin")
        {
            var students = await GetStudentsBySectionId(sectionId);
            return students;
        }

        // Teacher: Can only view students in their assigned sections
        if (requesterRole == "teacher")
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == requesterUserId);
            if (teacher == null)
            {
                throw new KeyNotFoundException("Teacher profile not found.");
            }

            var isTeacherInSection = await _context.SectionTeachers
                .AnyAsync(st => st.SectionId == sectionId && st.TeacherId == teacher.Id);

            if (!isTeacherInSection)
            {
                throw new UnauthorizedAccessException("You do not have access to this section's students.");
            }

            var students = await GetStudentsBySectionId(sectionId);
            return students;
        }

        throw new UnauthorizedAccessException("Access denied.");
    }

    // Helper method to fetch students by section ID
    private async Task<List<StudentBasicProfileDto>> GetStudentsBySectionId(int sectionId)
    {
        return await _context.Students
            .Include(s => s.Course)
            .Include(s => s.Section)
            .Where(s => s.SectionId == sectionId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentBasicProfileDto
            {
                Id = s.Id,
                StudentNumber = s.StudentNumber,
                FirstName = s.FirstName,
                LastName = s.LastName,
                MiddleName = s.MiddleName,
                YearLevel = s.YearLevel,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                CourseCode = s.Course != null ? s.Course.Code : null,
                SectionId = s.SectionId,
                SectionName = s.Section != null ? s.Section.Name : null
            })
            .ToListAsync();
    }

    // Maps Student entity to full profile DTO
    private static StudentProfileDto MapToFullProfile(Student student)
    {
        return new StudentProfileDto
        {
            Id = student.Id,
            UserId = student.UserId,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            MiddleName = student.MiddleName,
            Birthdate = student.Birthdate,
            Gender = student.Gender,
            Address = student.Address,
            GuardianName = student.GuardianName,
            GuardianContact = student.GuardianContact,
            YearLevel = student.YearLevel,
            CourseId = student.CourseId,
            CourseName = student.Course?.Name,
            CourseCode = student.Course?.Code,
            SectionId = student.SectionId,
            SectionName = student.Section?.Name,
            IsActive = student.IsActive
        };
    }

    // Maps Student entity to basic profile DTO (excludes sensitive data)
    private static StudentBasicProfileDto MapToBasicProfile(Student student)
    {
        return new StudentBasicProfileDto
        {
            Id = student.Id,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            MiddleName = student.MiddleName,
            YearLevel = student.YearLevel,
            CourseId = student.CourseId,
            CourseName = student.Course?.Name,
            CourseCode = student.Course?.Code,
            SectionId = student.SectionId,
            SectionName = student.Section?.Name
        };
    }
}


