using System.ComponentModel.DataAnnotations;
using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.DTOs.Staff;

public class CreateStaffRequest
{
    [Required, MaxLength(20)]
    public string EmployeeIdNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? ContactNumber { get; set; }

    [Required]
    public StaffType StaffType { get; set; }

    [Required]
    public Guid DepartmentId { get; set; }
}

public class UpdateStaffRequest
{
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [EmailAddress, MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? ContactNumber { get; set; }

    public StaffType? StaffType { get; set; }

    public Guid? DepartmentId { get; set; }
}

public class StaffResponse
{
    public Guid Id { get; set; }
    public string EmployeeIdNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName}, {FirstName}"
        : $"{LastName}, {FirstName} {MiddleName}";
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }
    public string? QrCodeData { get; set; }
    public string StaffType { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
