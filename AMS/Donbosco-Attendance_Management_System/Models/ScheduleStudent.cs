using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Donbosco_Attendance_Management_System.Models;

[Table("schedule_students")]
public class ScheduleStudent
{
    [Key]
    [Column("schedule_id", Order = 0)]
    public Guid ScheduleId { get; set; }

    [Key]
    [Column("student_id", Order = 1)]
    public Guid StudentId { get; set; }

    // Navigation properties
    [ForeignKey("ScheduleId")]
    public Schedule? Schedule { get; set; }

    [ForeignKey("StudentId")]
    public Student? Student { get; set; }
}