namespace Attendance_Management_System.Backend.ViewModels.Programs;

public class ProgramsIndexViewModel
{
    public IReadOnlyList<ProgramListItemViewModel> Programs { get; set; } = [];
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
