namespace WebApplication1.Models.Entities;

public class Subject
{
    public int Id { get; set; }
    public string TeacherId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ApplicationUser Teacher { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
