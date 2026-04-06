using Attendance_Management_System.Backend.ViewModels.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize(Policy = "AdminOrTeacher")]
[Route("attendance")]
public class AttendanceManagementController : Controller
{
    [HttpGet("")]
    public IActionResult Index([FromQuery] int? sectionId, [FromQuery] int? scheduleId, [FromQuery] DateOnly? date)
    {
        return RedirectToAction("Index", "SectionManagement", new
        {
            sectionId,
            scheduleId,
            attendanceDate = (date ?? DateOnly.FromDateTime(DateTime.Today)).ToString("yyyy-MM-dd")
        });
    }

    [HttpPost("mark")]
    [ValidateAntiForgeryToken]
    public IActionResult Mark([Bind(Prefix = "MarkForm")] MarkAttendanceFormViewModel form)
    {
        TempData["SectionAttendanceError"] = "The standalone attendance page is retired. Use the section checklist to mark or correct attendance.";

        return RedirectToAction("Index", "SectionManagement", new
        {
            sectionId = form.SectionId,
            scheduleId = form.ScheduleId,
            attendanceDate = form.Date.ToString("yyyy-MM-dd")
        });
    }
}
