using System.ComponentModel.DataAnnotations;
using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.DTOs.Students;

public class CreateStudentRequest
{
    [Required, MaxLength(20)]
    public string StudentIdNumber { get; set; } = string.Empty;

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
    public Guid SectionId { get; set; }
}

public class UpdateStudentRequest
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

    public EnrollmentStatus? EnrollmentStatus { get; set; }

    public Guid? SectionId { get; set; }
}

public class StudentResponse
{
    public Guid Id { get; set; }
    public string StudentIdNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? $"{LastName}, {FirstName}"
        : $"{LastName}, {FirstName} {MiddleName}";
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }
    public string? QrCodeData { get; set; }
    public string EnrollmentStatus { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string AcademicProgramName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
