using Attendance_Management_System.Backend.DTOs.Requests;
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
        var viewModel = await BuildIndexViewModelAsync();
        return View(viewModel);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateForm")] CreateAcademicPeriodFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        var result = await _academicYearsService.CreateAcademicYearAsync(new CreateAcademicYearRequest
        {
            YearLabel = form.YearLabel.Trim(),
            StartDate = form.StartDate,
            EndDate = form.EndDate
        });

        if (!result.Success)
        {
            ModelState.AddModelError("CreateForm.YearLabel", result.Error?.Message ?? "Unable to create academic period right now.");
            return View(nameof(Index), viewModel);
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [Bind(Prefix = "UpdateForm")] UpdateAcademicPeriodFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["AcademicPeriodsError"] = "Please provide valid academic period values.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _academicYearsService.UpdateAcademicYearAsync(id, new UpdateAcademicYearRequest
        {
            YearLabel = form.YearLabel.Trim(),
            StartDate = form.StartDate,
            EndDate = form.EndDate
        });

        if (!result.Success)
        {
            TempData["AcademicPeriodsError"] = result.Error?.Message ?? "Unable to update academic period right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _academicYearsService.DeleteAcademicYearAsync(id);

        if (!result.Success)
        {
            TempData["AcademicPeriodsError"] = result.Error?.Message ?? "Unable to delete academic period right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _academicYearsService.ActivateAcademicYearAsync(id);

        if (!result.Success)
        {
            TempData["AcademicPeriodsError"] = result.Error?.Message ?? "Unable to activate academic period right now.";
            return RedirectToAction(nameof(Index));
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period activated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AcademicPeriodsIndexViewModel> BuildIndexViewModelAsync()
    {
        var result = await _academicYearsService.GetAllAcademicYearsAsync();
        var viewModel = new AcademicPeriodsIndexViewModel();

        if (!result.Success || result.Data is null)
        {
            viewModel.ErrorMessage = result.Error?.Message ?? "Unable to load academic periods right now.";
            return viewModel;
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

        return viewModel;
    }
}
