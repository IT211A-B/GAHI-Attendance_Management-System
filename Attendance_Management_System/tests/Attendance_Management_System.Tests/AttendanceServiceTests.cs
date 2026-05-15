namespace Attendance_Management_System.Tests;

public class AttendanceServiceTests
{
    [Fact]
    public async Task MarkAttendanceAsync_UsesSectionAcademicYear_WhenCreatingFirstAttendanceRow()
    {
        await using var context = CreateContext();
        SeedAttendanceLookupRows(context);

        var service = CreateService(context);

        var result = await service.MarkAttendanceAsync(
            new MarkAttendanceRequest
            {
                SectionId = 30,
                ScheduleId = 40,
                StudentId = 50,
                Date = new DateOnly(2026, 5, 11),
                TimeIn = new TimeOnly(8, 5)
            },
            new TeacherContext { UserId = 10, IsAdmin = true });

        var attendance = await context.Attendances.SingleAsync();
        var audit = await context.AttendanceAudits.SingleAsync();
        Assert.Equal(100, attendance.AcademicYearId);
        Assert.Equal(attendance.Id, audit.AttendanceId);
        Assert.Equal("Present", result.StatusLabel);
    }

    [Fact]
    public async Task MarkBulkAttendanceAsync_UsesSectionAcademicYear_WhenCreatingFirstAttendanceRows()
    {
        await using var context = CreateContext();
        SeedAttendanceLookupRows(context);

        var service = CreateService(context);

        var result = await service.MarkBulkAttendanceAsync(
            new BulkAttendanceRequest
            {
                SectionId = 30,
                ScheduleId = 40,
                Date = new DateOnly(2026, 5, 11),
                Entries =
                [
                    new SingleAttendanceEntry
                    {
                        StudentId = 50,
                        TimeIn = new TimeOnly(8, 5)
                    }
                ]
            },
            new TeacherContext { UserId = 10, IsAdmin = true });

        var attendance = await context.Attendances.SingleAsync();
        Assert.Equal(100, attendance.AcademicYearId);
        Assert.Equal("Present", result.Single().StatusLabel);
    }

    [Fact]
    public async Task MarkAttendanceAsync_AllowsOffScheduleDates_WhenSettingEnabled()
    {
        await using var context = CreateContext();
        SeedAttendanceLookupRows(context);

        var service = CreateService(context, new AttendanceSettings
        {
            LateGraceMinutes = 15,
            TeacherBackfillDays = 7,
            AllowOffScheduleAttendance = true,
            TimezoneId = "Asia/Manila"
        });

        var date = new DateOnly(2026, 5, 12);
        var schedule = await context.Schedules.SingleAsync();
        schedule.DayOfWeek = ((int)date.DayOfWeek + 1) % 7;
        await context.SaveChangesAsync();

        var result = await service.MarkAttendanceAsync(
            new MarkAttendanceRequest
            {
                SectionId = 30,
                ScheduleId = 40,
                StudentId = 50,
                Date = date,
                TimeIn = new TimeOnly(8, 5)
            },
            new TeacherContext { UserId = 10, IsAdmin = true });

        Assert.Equal("Present", result.StatusLabel);
    }

    [Fact]
    public async Task MarkAttendanceAsync_RejectsOffScheduleDates_WhenSettingDisabled()
    {
        await using var context = CreateContext();
        SeedAttendanceLookupRows(context);

        var service = CreateService(context, new AttendanceSettings
        {
            LateGraceMinutes = 15,
            TeacherBackfillDays = 7,
            AllowOffScheduleAttendance = false,
            TimezoneId = "Asia/Manila"
        });

        var date = new DateOnly(2026, 5, 12);
        var schedule = await context.Schedules.SingleAsync();
        schedule.DayOfWeek = ((int)date.DayOfWeek + 1) % 7;
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkAttendanceAsync(
            new MarkAttendanceRequest
            {
                SectionId = 30,
                ScheduleId = 40,
                StudentId = 50,
                Date = date,
                TimeIn = new TimeOnly(8, 5)
            },
            new TeacherContext { UserId = 10, IsAdmin = true }));

        Assert.Equal("Attendance date must match the schedule weekday.", exception.Message);
    }

    private static AttendanceService CreateService(AppDbContext context, AttendanceSettings? settings = null)
    {
        return new AttendanceService(context, Options.Create(settings ?? AttendanceSettings.Default));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedAttendanceLookupRows(AppDbContext context)
    {
        context.Users.AddRange(
            new User { Id = 10, UserName = "admin@example.test", Email = "admin@example.test", Role = "admin" },
            new User { Id = 11, UserName = "student@example.test", Email = "student@example.test", Role = "student" });

        context.Teachers.Add(new Teacher
        {
            Id = 20,
            UserId = 10,
            EmployeeNumber = "T-001",
            FirstName = "Ada",
            LastName = "Lovelace",
            Department = "IT",
            IsActive = true
        });

        context.AcademicYears.Add(new AcademicYear
        {
            Id = 100,
            YearLabel = "2026-2027",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2027, 3, 31),
            IsActive = false
        });

        context.Courses.Add(new Course
        {
            Id = 200,
            Name = "Information Technology",
            Code = "IT",
            EducationLevel = EducationLevel.College
        });

        context.Subjects.Add(new Subject
        {
            Id = 300,
            CourseId = 200,
            Name = "Programming",
            Code = "IT101"
        });

        context.Classrooms.Add(new Classroom
        {
            Id = 400,
            Name = "Lab 1"
        });

        context.Sections.Add(new Section
        {
            Id = 30,
            Name = "BSIT-1A",
            YearLevel = 1,
            AcademicYearId = 100,
            CourseId = 200,
            SubjectId = 300,
            ClassroomId = 400
        });

        context.Students.Add(new Student
        {
            Id = 50,
            UserId = 11,
            CourseId = 200,
            SectionId = 30,
            StudentNumber = "S-001",
            FirstName = "Grace",
            LastName = "Hopper",
            Birthdate = new DateOnly(2008, 1, 1),
            Address = "Test Address",
            GuardianName = "Guardian",
            GuardianContact = "123",
            YearLevel = 1,
            IsActive = true
        });

        context.Schedules.Add(new Schedule
        {
            Id = 40,
            SectionId = 30,
            TeacherId = 20,
            SubjectId = 300,
            DayOfWeek = (int)DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            EffectiveFrom = new DateOnly(2026, 5, 1)
        });

        context.SaveChanges();
    }
}
