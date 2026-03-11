namespace WebApplication1.Models.Entities;

public class Student
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string StudentNo { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;

    public Subject Subject { get; set; } = null!;
    public ICollection<ClassAttendance> Attendances { get; set; } = new List<ClassAttendance>();
}
