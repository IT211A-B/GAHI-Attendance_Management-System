using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

// Centralized section allocation policy used by signup and student enrollment flows
public class SectionAllocationService : ISectionAllocationService
{
    private static readonly string ApprovedEnrollmentStatus = EnrollmentStatus.Approved.ToStorageValue();

    private readonly AppDbContext _context;
    private readonly EnrollmentSettings _enrollmentSettings;

    public SectionAllocationService(AppDbContext context, IOptions<EnrollmentSettings> enrollmentSettings)
    {
        _context = context;
        _enrollmentSettings = enrollmentSettings.Value?.IsValid() == true
            ? enrollmentSettings.Value
            : EnrollmentSettings.Default;
    }

    public async Task<Section?> AllocateSectionAsync(int courseId, int academicYearId, int yearLevel)
    {
        // Reject mismatched course or year-level requests before touching section inventory.
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(selectedCourse => selectedCourse.Id == courseId);

        if (course == null || !EducationLevelPolicy.IsYearLevelAllowed(course.EducationLevel, yearLevel))
        {
            return null;
        }

        // Only sections in the same course, academic year, and year level can be reused.
        var sections = await _context.Sections
            .Where(s => s.CourseId == courseId && s.AcademicYearId == academicYearId && s.YearLevel == yearLevel)
            .ToListAsync();

        // Prefer an existing seat before considering new section creation.
        var bestSection = await SelectBestSectionAsync(sections);
        if (bestSection != null)
        {
            return bestSection;
        }

        // Respect settings that intentionally disable automatic section growth.
        if (!_enrollmentSettings.AutoCreateSections)
        {
            return null;
        }

        return await AutoCreateSectionIfNeededAsync(courseId, yearLevel, academicYearId, sections);
    }

    private async Task<Section?> SelectBestSectionAsync(List<Section> sections)
    {
        if (!sections.Any())
        {
            return null;
        }

        var sectionIds = sections.Select(s => s.Id).ToList();
        // Measure load using approved enrollments only; pending requests should not block allocation.
        var enrollmentCounts = await _context.Enrollments
            .Where(e => sectionIds.Contains(e.SectionId) && e.Status == ApprovedEnrollmentStatus)
            .GroupBy(e => e.SectionId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var candidates = sections
            .Select(section => new
            {
                Section = section,
                ApprovedCount = enrollmentCounts.GetValueOrDefault(section.Id, 0)
            })
            .Where(x => x.ApprovedCount < _enrollmentSettings.OverCapacityLimit)
            .ToList();

        if (!candidates.Any())
        {
            return null;
        }

        if (!_enrollmentSettings.DeterministicSectionAssignment)
        {
            // Random assignment spreads students across eligible sections when deterministic mode is off.
            var randomIndex = Random.Shared.Next(candidates.Count);
            return candidates[randomIndex].Section;
        }

        // Prefer sections below warning threshold, then least loaded, then stable name ordering.
        return candidates
            .OrderBy(x => x.ApprovedCount >= _enrollmentSettings.WarningThreshold ? 1 : 0)
            .ThenBy(x => x.ApprovedCount)
            .ThenBy(x => x.Section.Name)
            .Select(x => x.Section)
            .FirstOrDefault();
    }

    private async Task<Section?> AutoCreateSectionIfNeededAsync(
        int courseId,
        int yearLevel,
        int academicYearId,
        List<Section> existingSections)
    {
        if (existingSections.Any())
        {
            // If at least one section still has space, reuse it instead of creating a new one.
            var existingSectionIds = existingSections.Select(s => s.Id).ToList();
            var enrollmentCounts = await _context.Enrollments
                .Where(e => existingSectionIds.Contains(e.SectionId) && e.Status == ApprovedEnrollmentStatus)
                .GroupBy(e => e.SectionId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var hasAvailableSection = existingSections.Any(section =>
                enrollmentCounts.GetValueOrDefault(section.Id, 0) < _enrollmentSettings.OverCapacityLimit);

            if (hasAvailableSection)
            {
                return null;
            }
        }

        // New sections need a course, a classroom, and a subject before they can be created safely.
        var course = await _context.Courses.FindAsync(courseId);
        var classroom = await _context.Classrooms.FirstOrDefaultAsync();
        var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.CourseId == courseId);

        if (course == null || classroom == null || subject == null)
        {
            return null;
        }

        // Keep the generated section name unique within the current course/year-level set.
        var existingNames = existingSections
            .Select(section => section.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sectionNumber = existingSections.Count + 1;
        var proposedName = $"{course.Code}-{yearLevel}-{sectionNumber}";

        while (existingNames.Contains(proposedName))
        {
            sectionNumber++;
            proposedName = $"{course.Code}-{yearLevel}-{sectionNumber}";
        }

        var newSection = new Section
        {
            Name = proposedName,
            YearLevel = yearLevel,
            CourseId = courseId,
            SubjectId = subject.Id,
            AcademicYearId = academicYearId,
            ClassroomId = classroom.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Sections.Add(newSection);
        await _context.SaveChangesAsync();
        return newSection;
    }
}