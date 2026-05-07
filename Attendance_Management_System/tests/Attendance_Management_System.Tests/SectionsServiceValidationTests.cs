namespace Attendance_Management_System.Tests;

public class SectionsServiceValidationTests
{
    [Fact]
    public async Task CreateSectionAsync_ReturnsValidationError_WhenYearLevelOutsideCourseRange()
    {
        await using var context = CreateContext();
        var service = new SectionsService(context, Options.Create(EnrollmentSettings.Default));

        var academicYear = new AcademicYear
        {
            Id = 1,
            YearLabel = "2026-2027",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2027, 3, 31),
            IsActive = true
        };

        var course = new Course
        {
            Id = 20,
            Name = "Senior High School - STEM",
            Code = "SHSSTEM",
            EducationLevel = EducationLevel.SeniorHigh
        };

        var subject = new Subject
        {
            Id = 30,
            Name = "Pre-Calculus",
            Code = "SHSPRECALC",
            CourseId = course.Id,
            Units = 3
        };

        var classroom = new Classroom
        {
            Id = 40,
            Name = "SHS Room A",
            Description = "Senior High classroom"
        };

        context.AcademicYears.Add(academicYear);
        context.Courses.Add(course);
        context.Subjects.Add(subject);
        context.Classrooms.Add(classroom);
        await context.SaveChangesAsync();

        var response = await service.CreateSectionAsync(new CreateSectionRequest
        {
            Name = "SHS-9A",
            YearLevel = 9,
            AcademicYearId = academicYear.Id,
            CourseId = course.Id,
            SubjectId = subject.Id,
            ClassroomId = classroom.Id
        });

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal("VALIDATION_ERROR", response.Error!.Code);
        Assert.Equal("Year level 9 is not valid for Senior High. Allowed range is 11-12.", response.Error.Message);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
