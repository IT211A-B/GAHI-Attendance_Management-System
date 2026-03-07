namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// An institutional department (e.g., College, TVET, Senior High School).
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<AcademicProgram> AcademicPrograms { get; set; } = new List<AcademicProgram>();
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
    public ICollection<BusinessRule> BusinessRules { get; set; } = new List<BusinessRule>();
}
