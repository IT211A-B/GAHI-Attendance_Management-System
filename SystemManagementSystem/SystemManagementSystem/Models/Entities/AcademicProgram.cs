namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// An academic program under a department (e.g., BSIT, BSCS, TVET-Cookery).
/// Named "AcademicProgram" to avoid collision with the C# keyword "Program".
/// </summary>
public class AcademicProgram : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Foreign Keys
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    // Navigation
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
