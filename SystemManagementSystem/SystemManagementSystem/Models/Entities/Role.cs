namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A named role for Role-Based Access Control (e.g., Admin, Guard, Registrar, DepartmentHead, Staff).
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
