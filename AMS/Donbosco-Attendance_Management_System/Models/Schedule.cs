using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donbosco_Attendance_Management_System.Models;

[Table("schedules")]
public class Schedule
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("section_id")]
    public Guid SectionId { get; set; }

    [Required]
    [Column("teacher_id")]
    public Guid TeacherId { get; set; }

    [Required]
    [Column("classroom_id")]
    public Guid ClassroomId { get; set; }

    [Required]
    [Column("subject_name")]
    [MaxLength(255)]
    public string SubjectName { get; set; } = string.Empty;

    [Required]
    [Column("day_of_week")]
    public int DayOfWeek { get; set; } // 0=Mon, 1=Tue, 2=Wed, 3=Thu, 4=Fri, 5=Sat, 6=Sun

    [Required]
    [Column("time_in")]
    public TimeOnly TimeIn { get; set; }

    [Required]
    [Column("time_out")]
    public TimeOnly TimeOut { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SectionId")]
    public Section? Section { get; set; }

    [ForeignKey("TeacherId")]
    public User? Teacher { get; set; }

    [ForeignKey("ClassroomId")]
    public Classroom? Classroom { get; set; }

    public ICollection<ScheduleStudent>? ScheduleStudents { get; set; }
    public ICollection<Attendance>? Attendances { get; set; }
}