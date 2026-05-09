namespace Attendance_Management_System.Tests;

public class SectionPageServiceTests
{
    [Fact]
    public async Task ValidateSubjectSelectionForSectionAsync_ReturnsInvalid_WhenSubjectIdIsNotPositive()
    {
        var service = CreateService();

        var result = await service.ValidateSubjectSelectionForSectionAsync(sectionId: 1, subjectId: 0);

        Assert.False(result.IsValid);
        Assert.Equal("Select a subject before adding a timetable slot.", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateSubjectSelectionForSectionAsync_ReturnsValid_WhenSectionAndSubjectExist()
    {
        var sectionsService = new Mock<ISectionsService>();
        sectionsService
            .Setup(service => service.GetSectionByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SectionDto { Id = 1, Name = "A" });

        var subjectsService = new Mock<ISubjectsService>();
        subjectsService
            .Setup(service => service.GetSubjectByIdAsync(2))
            .ReturnsAsync(new SubjectDto { Id = 2, Name = "Math", Code = "MTH101", CourseId = 10 });

        var service = CreateService(
            sectionsService: sectionsService,
            subjectsService: subjectsService);

        var result = await service.ValidateSubjectSelectionForSectionAsync(sectionId: 1, subjectId: 2);

        Assert.True(result.IsValid);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public async Task BuildTeacherContextAsync_ReturnsAdminContext_WhenUserIsAdmin()
    {
        var service = CreateService();

        var result = await service.BuildTeacherContextAsync(userId: 99, role: "admin");

        Assert.True(result.Success);
        Assert.True(result.Context.IsAdmin);
        Assert.Null(result.Context.TeacherId);
        Assert.Equal(99, result.Context.UserId);
    }

    [Fact]
    public async Task BuildTeacherContextAsync_ReturnsError_WhenTeacherProfileIsMissing()
    {
        var teachersService = new Mock<ITeachersService>();
        teachersService
            .Setup(service => service.GetAllTeachersAsync())
            .ReturnsAsync([]);

        var service = CreateService(teachersService: teachersService);

        var result = await service.BuildTeacherContextAsync(userId: 99, role: "teacher");

        Assert.False(result.Success);
        Assert.Equal("Teacher profile not found for the current account.", result.Error);
    }

    private static SectionPageService CreateService(
        Mock<ISectionsService>? sectionsService = null,
        Mock<ITeachersService>? teachersService = null,
        Mock<ISubjectsService>? subjectsService = null)
    {
        return new SectionPageService(
            sectionsService?.Object ?? Mock.Of<ISectionsService>(),
            Mock.Of<ISchedulesService>(),
            Mock.Of<IStudentsService>(),
            Mock.Of<IAttendanceService>(),
            teachersService?.Object ?? Mock.Of<ITeachersService>(),
            Mock.Of<IAcademicYearsService>(),
            Mock.Of<ICoursesService>(),
            subjectsService?.Object ?? Mock.Of<ISubjectsService>(),
            Mock.Of<IClassroomsService>());
    }
}
