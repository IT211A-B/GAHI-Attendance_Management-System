using System.ComponentModel.DataAnnotations;

namespace SystemManagementSystem.DTOs.Sections;

public class CreateSectionRequest
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, Range(1, 6)]
    public int YearLevel { get; set; }

    [Required]
    public Guid AcademicProgramId { get; set; }

    [Required]
    public Guid AcademicPeriodId { get; set; }
}

public class UpdateSectionRequest
{
    [MaxLength(50)]
    public string? Name { get; set; }

    [Range(1, 6)]
    public int? YearLevel { get; set; }

    public Guid? AcademicProgramId { get; set; }
    public Guid? AcademicPeriodId { get; set; }
}

public class SectionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public Guid AcademicProgramId { get; set; }
    public string AcademicProgramName { get; set; } = string.Empty;
    public Guid AcademicPeriodId { get; set; }
    public string AcademicPeriodName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
