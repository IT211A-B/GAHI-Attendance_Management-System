using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.ViewModels.AcademicPeriods;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOnly")]
[Route("academic-periods")]
public class AcademicPeriodsController : Controller
{
    private readonly IAcademicYearsService _academicYearsService;

    public AcademicPeriodsController(IAcademicYearsService academicYearsService)
    {
        _academicYearsService = academicYearsService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _academicYearsService.GetAllAcademicYearsAsync();
        var viewModel = new AcademicPeriodsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load academic periods right now.";
            return View(viewModel);
        }

        viewModel.Periods = result.Data
            .OrderByDescending(p => p.StartDate)
            .Select(period => new AcademicPeriodListItemViewModel
            {
                Id = period.Id,
                YearLabel = period.YearLabel,
                StartDate = period.StartDate.ToString("yyyy-MM-dd"),
                EndDate = period.EndDate.ToString("yyyy-MM-dd"),
                IsActive = period.IsActive
            })
            .ToList();

        return View(viewModel);
    }
}
