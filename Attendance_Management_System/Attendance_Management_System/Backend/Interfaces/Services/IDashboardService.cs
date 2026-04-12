using Attendance_Management_System.Backend.ViewModels.Dashboard;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardIndexViewModel> BuildIndexViewModelAsync(int userId, string role, string? window, DateOnly? from, DateOnly? to);
}
