using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a class section that combines academic year, course, subject, and classroom
public class Section : EntityBase
{
    public string Name { get; set; } = string.Empty;

    // Foreign keys linking to related entities
    public int AcademicYearId { get; set; }
    public int CourseId { get; set; }
    public int SubjectId { get; set; }
    public int ClassroomId { get; set; }

    // Navigation properties for related entities
    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [ForeignKey(nameof(SubjectId))]
    public Subject? Subject { get; set; }

    [ForeignKey(nameof(ClassroomId))]
    public Classroom? Classroom { get; set; }

    // Teachers assigned to this section (many-to-many via SectionTeacher bridge)
    public ICollection<SectionTeacher> SectionTeachers { get; set; } = new List<SectionTeacher>();
}
