using System.Security.Claims;
using Attendance_Management_System.Backend.Controllers;
using Attendance_Management_System.Backend.ViewModels.Sections;
using Microsoft.Extensions.Logging.Abstractions;

namespace Attendance_Management_System.Tests;

public class SectionAttendanceControllerTests
{
    [Fact]
    public async Task MarkSectionAttendance_ShowsServiceValidationMessage_WhenMarkingFails()
    {
        const string validationMessage = "Attendance date must match the schedule weekday.";

        var sectionPageService = new Mock<ISectionPageService>();
        sectionPageService
            .Setup(service => service.BuildTeacherContextAsync(10, "teacher"))
            .ReturnsAsync((true, new TeacherContext { UserId = 10, TeacherId = 20 }, null));

        var attendanceService = new Mock<IAttendanceService>();
        attendanceService
            .Setup(service => service.MarkAttendanceAsync(
                It.IsAny<MarkAttendanceRequest>(),
                It.IsAny<TeacherContext>()))
            .ThrowsAsync(new InvalidOperationException(validationMessage));

        var controller = new SectionAttendanceController(
            sectionPageService.Object,
            attendanceService.Object,
            NullLogger<SectionAttendanceController>.Instance);
        SetAuthenticatedContext(controller);

        var result = await controller.MarkSectionAttendance(new SectionMarkAttendanceFormViewModel
        {
            SectionId = 30,
            ScheduleId = 40,
            StudentId = 50,
            Date = new DateOnly(2026, 5, 12),
            TimeIn = new TimeOnly(8, 5)
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(SectionAttendanceController.Index), redirect.ActionName);
        Assert.Equal(validationMessage, controller.TempData["SectionAttendanceError"]);
    }

    private static void SetAuthenticatedContext(Controller controller)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "10"),
                new Claim(ClaimTypes.Role, "teacher")
            ]))
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
    }
}
