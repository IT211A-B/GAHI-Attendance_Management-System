using System;
using System.ComponentModel.DataAnnotations;
using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.ViewModels.Enrollments;

public class EnrollmentsIndexViewModel
{
    public bool IsAdmin { get; set; }
    public bool IsStudent { get; set; }

    public string? ErrorMessage { get; set; }

    public string? SelectedStatus { get; set; }
    public int? SelectedAcademicYearId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page * PageSize < TotalCount;

    public IReadOnlyList<EnrollmentListItemViewModel> Enrollments { get; set; } = [];
    public IReadOnlyList<EnrollmentOptionViewModel> AcademicYears { get; set; } = [];
    public IReadOnlyList<EnrollmentOptionViewModel> Sections { get; set; } = [];
    public IReadOnlyList<EnrollmentOptionViewModel> Courses { get; set; } = [];

    public StudentEnrollmentProfileViewModel? StudentProfile { get; set; }
    public CreateEnrollmentFormViewModel CreateForm { get; set; } = new();
    public IReadOnlyList<SectionCapacityItemViewModel> AvailableSections { get; set; } = [];
}

public class EnrollmentListItemViewModel
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "-";
    public string StudentNumber { get; set; } = "-";
    public int SectionId { get; set; }
    public string SectionName { get; set; } = "-";
    public int AcademicYearId { get; set; }
    public string AcademicYearLabel { get; set; } = "-";
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string ProcessorName { get; set; } = "-";
    public string? RejectionReason { get; set; }
    public bool HasWarning { get; set; }
    public string? WarningMessage { get; set; }

    public bool IsPending => Status.Equals("pending", StringComparison.OrdinalIgnoreCase);
    public bool CanReassign => !Status.Equals("rejected", StringComparison.OrdinalIgnoreCase);
}

public class EnrollmentOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class StudentEnrollmentProfileViewModel
{
    public string StudentNumber { get; set; } = "-";
    public string FullName { get; set; } = "-";
    public int YearLevel { get; set; }
    public int? CourseId { get; set; }
    public string CourseText { get; set; } = "-";
}

public class SectionCapacityItemViewModel
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public int CurrentEnrollment { get; set; }
    public int AvailableSlots { get; set; }
    public SectionCapacityStatus Status { get; set; }

    public string StatusText => Status switch
    {
        SectionCapacityStatus.Available => "Available",
        SectionCapacityStatus.AtWarning => "Near Capacity",
        SectionCapacityStatus.OverCapacity => "Over Capacity",
        _ => "Unknown"
    };
}

public class CreateEnrollmentFormViewModel
{
    [Required(ErrorMessage = "Course is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Academic period is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid academic period")]
    [Display(Name = "Academic Period")]
    public int AcademicYearId { get; set; }
}

public class RejectEnrollmentFormViewModel
{
    [Required(ErrorMessage = "Rejection reason is required")]
    [StringLength(240, ErrorMessage = "Reason must be 240 characters or fewer")]
    [Display(Name = "Rejection Reason")]
    public string RejectionReason { get; set; } = string.Empty;
}

public class ReassignEnrollmentFormViewModel
{
    [Required(ErrorMessage = "Target section is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid section")]
    [Display(Name = "New Section")]
    public int NewSectionId { get; set; }

    [StringLength(240, ErrorMessage = "Reason must be 240 characters or fewer")]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }
}
