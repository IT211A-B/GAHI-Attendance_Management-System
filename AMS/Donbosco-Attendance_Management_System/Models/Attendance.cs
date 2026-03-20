using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donbosco_Attendance_Management_System.Models;

[Table("attendance")]
public class Attendance
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("schedule_id")]
    public Guid ScheduleId { get; set; }

    [Required]
    [Column("student_id")]
    public Guid StudentId { get; set; }

    [Required]
    [Column("date")]
    public DateOnly Date { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "present"; // "present", "absent", "late"

    [Required]
    [Column("marked_by")]
    public Guid MarkedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ScheduleId")]
    public Schedule? Schedule { get; set; }

    [ForeignKey("StudentId")]
    public Student? Student { get; set; }

    [ForeignKey("MarkedBy")]
    public User? MarkedByUser { get; set; }
}