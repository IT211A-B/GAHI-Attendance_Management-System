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
    public async Task<IActionResult> Create(CreateAcademicPeriodFormViewModel form)
    {
        var viewModel = await BuildIndexViewModelAsync();
        viewModel.CreateForm = form;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), viewModel);
        }

        try
        {
            await _academicYearsService.CreateAcademicYearAsync(new CreateAcademicYearRequest
            {
                YearLabel = form.YearLabel.Trim(),
                StartDate = form.StartDate,
                EndDate = form.EndDate
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("CreateForm.YearLabel", string.IsNullOrWhiteSpace(ex.Message) ? "Unable to create academic period right now." : ex.Message);
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

        try
        {
            await _academicYearsService.UpdateAcademicYearAsync(id, new UpdateAcademicYearRequest
            {
                YearLabel = form.YearLabel.Trim(),
                StartDate = form.StartDate,
                EndDate = form.EndDate
            });
        }
        catch (Exception ex)
        {
            TempData["AcademicPeriodsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to update academic period right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _academicYearsService.DeleteAcademicYearAsync(id);
        }
        catch (Exception ex)
        {
            TempData["AcademicPeriodsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to delete academic period right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            await _academicYearsService.ActivateAcademicYearAsync(id);
        }
        catch (Exception ex)
        {
            TempData["AcademicPeriodsError"] = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to activate academic period right now." : ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["AcademicPeriodsSuccess"] = "Academic period activated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AcademicPeriodsIndexViewModel> BuildIndexViewModelAsync()
    {
        var viewModel = new AcademicPeriodsIndexViewModel();

        try
        {
            var periods = await _academicYearsService.GetAllAcademicYearsAsync();

            viewModel.Periods = periods
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
        }
        catch (Exception ex)
        {
            viewModel.ErrorMessage = string.IsNullOrWhiteSpace(ex.Message) ? "Unable to load academic periods right now." : ex.Message;
        }

        return viewModel;
    }
}
