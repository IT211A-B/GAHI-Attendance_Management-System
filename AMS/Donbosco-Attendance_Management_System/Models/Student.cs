using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donbosco_Attendance_Management_System.Models;

[Table("students")]
public class Student
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("section_id")]
    public Guid SectionId { get; set; }

    [Column("is_irregular")]
    public bool IsIrregular { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SectionId")]
    public Section? Section { get; set; }
    public ICollection<ScheduleStudent>? ScheduleStudents { get; set; }
    public ICollection<Attendance>? Attendances { get; set; }
}