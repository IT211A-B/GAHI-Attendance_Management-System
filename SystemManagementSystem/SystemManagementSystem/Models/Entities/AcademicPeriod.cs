namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// Defines an academic period / semester / term (e.g., "SY 2025-2026 1st Semester").
/// </summary>
public class AcademicPeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;

    // Navigation
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
