namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A section within an academic program for a given academic period (e.g., BSIT-3A, SY 2025‒2026 Sem 1).
/// </summary>
public class Section : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int YearLevel { get; set; }

    // Foreign Keys
    public Guid AcademicProgramId { get; set; }
    public AcademicProgram AcademicProgram { get; set; } = null!;

    public Guid AcademicPeriodId { get; set; }
    public AcademicPeriod AcademicPeriod { get; set; } = null!;

    // Navigation
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
