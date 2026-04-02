using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.Classrooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("departments")]
[Route("classrooms")]
public class ClassroomManagementController : Controller
{
    private readonly IClassroomsService _classroomsService;

    public ClassroomManagementController(IClassroomsService classroomsService)
    {
        _classroomsService = classroomsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _classroomsService.GetAllClassroomsAsync();

        var viewModel = new ClassroomsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load classrooms right now.";
            return View(viewModel);
        }

        viewModel.Classrooms = result.Data
            .OrderBy(c => c.Name)
            .Select(classroom => new ClassroomListItemViewModel
            {
                Id = classroom.Id,
                Name = classroom.Name,
                Description = string.IsNullOrWhiteSpace(classroom.Description) ? "-" : classroom.Description
            })
            .ToList();

        return View(viewModel);
    }
}