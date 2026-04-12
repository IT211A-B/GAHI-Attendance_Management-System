using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Programs;

public class ProgramsIndexViewModel
{
    public IReadOnlyList<ProgramListItemViewModel> Programs { get; set; } = [];
    public CreateProgramFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ProgramListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = "-";
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateProgramFormViewModel
{
    [Required(ErrorMessage = "Code is required")]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}

public class UpdateProgramFormViewModel
{
    [Required(ErrorMessage = "Code is required")]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}
