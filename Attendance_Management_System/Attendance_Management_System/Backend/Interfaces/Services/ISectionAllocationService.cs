using Attendance_Management_System.Backend.Entities;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Provides a shared section auto-assignment policy for enrollment flows
public interface ISectionAllocationService
{
    // Finds the best section for the given course/year/academic period.
    // Returns null when no section can be allocated.
    Task<Section?> AllocateSectionAsync(int courseId, int academicYearId, int yearLevel);
}