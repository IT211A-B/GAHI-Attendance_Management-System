namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// Many-to-many join table: User ↔ Role.
/// </summary>
public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
