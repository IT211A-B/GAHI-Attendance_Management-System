using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class Section : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public int ClassroomId { get; set; }

    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [ForeignKey(nameof(SubjectId))]
    public Subject? Subject { get; set; }

    [ForeignKey(nameof(ClassroomId))]
    public Classroom? Classroom { get; set; }
}