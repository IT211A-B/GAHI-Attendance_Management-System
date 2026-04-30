using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Classrooms;

public class ClassroomsIndexViewModel
{
    public IReadOnlyList<ClassroomListItemViewModel> Classrooms { get; set; } = [];
    public CreateClassroomFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ClassroomListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = "-";
}

public class CreateClassroomFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}

public class UpdateClassroomFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }
}