using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Attendance_Management_System.Tests;

public class SchedulesServiceDeleteTests
{
    [Fact]
    public async Task DeleteScheduleAsync_ReturnsConflict_WhenAttendanceExists()
    {
        await using var context = CreateContext();
        var seed = await SeedScheduleAsync(context);

        context.Attendances.Add(new Attendance
        {
            Id = 1,
            ScheduleId = seed.ScheduleId,
            StudentId = 700,
            AcademicYearId = seed.AcademicYearId,
            SectionId = seed.SectionId,
            Date = new DateOnly(2026, 4, 7),
            MarkedBy = seed.OwnerUserId
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.DeleteScheduleAsync(seed.ScheduleId, seed.OwnerUserId, isAdmin: false);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Conflict, response.Error!.Code);
        Assert.Equal("Cannot delete schedule with existing attendance records.", response.Error.Message);
        Assert.True(await context.Schedules.AnyAsync(schedule => schedule.Id == seed.ScheduleId));
    }

    [Fact]
    public async Task DeleteScheduleAsync_ReturnsConflict_WhenActiveQrSessionExists()
    {
        await using var context = CreateContext();
        var seed = await SeedScheduleAsync(context);

        context.AttendanceQrSessions.Add(new AttendanceQrSession
        {
            Id = 1,
            SessionId = "session-1",
            SectionId = seed.SectionId,
            ScheduleId = seed.ScheduleId,
            SubjectId = seed.SubjectId,
            CreatedByUserId = seed.OwnerUserId,
            OwnerTeacherId = seed.OwnerTeacherId,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            IsActive = true,
            TokenNonce = "nonce-1",
            ClosedAtUtc = null
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.DeleteScheduleAsync(seed.ScheduleId, seed.OwnerUserId, isAdmin: false);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Conflict, response.Error!.Code);
        Assert.Equal("Cannot delete schedule with active or checked-in QR attendance sessions.", response.Error.Message);
        Assert.True(await context.Schedules.AnyAsync(schedule => schedule.Id == seed.ScheduleId));
    }

    [Fact]
    public async Task DeleteScheduleAsync_ReturnsConflict_WhenInactiveQrSessionHasCheckins()
    {
        await using var context = CreateContext();
        var seed = await SeedScheduleAsync(context);

        context.AttendanceQrSessions.Add(new AttendanceQrSession
        {
            Id = 1,
            SessionId = "session-1",
            SectionId = seed.SectionId,
            ScheduleId = seed.ScheduleId,
            SubjectId = seed.SubjectId,
            CreatedByUserId = seed.OwnerUserId,
            OwnerTeacherId = seed.OwnerTeacherId,
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-15),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsActive = false,
            TokenNonce = "nonce-1",
            ClosedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-4)
        });

        context.AttendanceQrCheckins.Add(new AttendanceQrCheckin
        {
            Id = 1,
            AttendanceQrSessionId = 1,
            StudentId = 700,
            CheckedInAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            Status = "present"
        });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.DeleteScheduleAsync(seed.ScheduleId, seed.OwnerUserId, isAdmin: false);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Conflict, response.Error!.Code);
        Assert.Equal("Cannot delete schedule with active or checked-in QR attendance sessions.", response.Error.Message);
        Assert.True(await context.Schedules.AnyAsync(schedule => schedule.Id == seed.ScheduleId));
    }

    [Fact]
    public async Task DeleteScheduleAsync_Succeeds_WhenOnlyInactiveOrExpiredEmptyQrSessionsExist()
    {
        await using var context = CreateContext();
        var seed = await SeedScheduleAsync(context);

        context.AttendanceQrSessions.AddRange(
            new AttendanceQrSession
            {
                Id = 1,
                SessionId = "session-inactive",
                SectionId = seed.SectionId,
                ScheduleId = seed.ScheduleId,
                SubjectId = seed.SubjectId,
                CreatedByUserId = seed.OwnerUserId,
                OwnerTeacherId = seed.OwnerTeacherId,
                IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
                IsActive = false,
                TokenNonce = "nonce-1",
                ClosedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-9)
            },
            new AttendanceQrSession
            {
                Id = 2,
                SessionId = "session-expired",
                SectionId = seed.SectionId,
                ScheduleId = seed.ScheduleId,
                SubjectId = seed.SubjectId,
                CreatedByUserId = seed.OwnerUserId,
                OwnerTeacherId = seed.OwnerTeacherId,
                IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                IsActive = true,
                TokenNonce = "nonce-2",
                ClosedAtUtc = null
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);
        var response = await service.DeleteScheduleAsync(seed.ScheduleId, seed.OwnerUserId, isAdmin: false);

        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.False(await context.Schedules.AnyAsync(schedule => schedule.Id == seed.ScheduleId));
        Assert.False(await context.AttendanceQrSessions.AnyAsync(session => session.ScheduleId == seed.ScheduleId));
    }

    [Fact]
    public async Task DeleteScheduleAsync_Succeeds_WhenNoAttendanceOrQrSessionsExist()
    {
        await using var context = CreateContext();
        var seed = await SeedScheduleAsync(context);

        var service = CreateService(context);
        var response = await service.DeleteScheduleAsync(seed.ScheduleId, seed.OwnerUserId, isAdmin: false);

        Assert.True(response.Success);
        Assert.True(response.Data);
        Assert.False(await context.Schedules.AnyAsync(schedule => schedule.Id == seed.ScheduleId));
    }

    [Fact]
    public async Task DeleteScheduleAsync_ReturnsForbidden_WhenCallerIsNotOwnerAndNotAdmin()
    {
        await using var context = CreateContext();
        var seed = await SeedScheduleAsync(context, includeNonOwnerTeacher: true);

        var service = CreateService(context);
        var response = await service.DeleteScheduleAsync(seed.ScheduleId, seed.OtherTeacherUserId!.Value, isAdmin: false);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.Forbidden, response.Error!.Code);
        Assert.Equal("You can only delete your own schedule slots.", response.Error.Message);
        Assert.True(await context.Schedules.AnyAsync(schedule => schedule.Id == seed.ScheduleId));
    }

    private static SchedulesService CreateService(AppDbContext context)
    {
        var conflictServiceMock = new Mock<IConflictService>(MockBehavior.Strict);

        return new SchedulesService(context, conflictServiceMock.Object);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<SeededScheduleData> SeedScheduleAsync(AppDbContext context, bool includeNonOwnerTeacher = false)
    {
        var academicYear = new AcademicYear
        {
            Id = 1,
            YearLabel = "2025-2026",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2026, 5, 31),
            IsActive = true
        };

        var course = new Course
        {
            Id = 1,
            Name = "Computer Science",
            Code = "BSCS"
        };

        var classroom = new Classroom
        {
            Id = 1,
            Name = "Room 101"
        };

        var subject = new Subject
        {
            Id = 1,
            Name = "Algorithms",
            Code = "CS101",
            CourseId = course.Id,
            Units = 3
        };

        var section = new Section
        {
            Id = 1,
            Name = "Section A",
            YearLevel = 1,
            AcademicYearId = academicYear.Id,
            CourseId = course.Id,
            SubjectId = subject.Id,
            ClassroomId = classroom.Id
        };

        var ownerTeacher = new Teacher
        {
            Id = 10,
            UserId = 100,
            EmployeeNumber = "T-100",
            FirstName = "Ada",
            LastName = "Owner",
            Department = "College"
        };

        var schedule = new Schedule
        {
            Id = 50,
            SectionId = section.Id,
            TeacherId = ownerTeacher.Id,
            SubjectId = subject.Id,
            DayOfWeek = (int)DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            EffectiveFrom = new DateOnly(2026, 1, 1)
        };

        context.AcademicYears.Add(academicYear);
        context.Courses.Add(course);
        context.Classrooms.Add(classroom);
        context.Subjects.Add(subject);
        context.Sections.Add(section);
        context.Teachers.Add(ownerTeacher);
        context.SectionTeachers.Add(new SectionTeacher
        {
            SectionId = section.Id,
            TeacherId = ownerTeacher.Id
        });
        context.Schedules.Add(schedule);

        int? otherTeacherUserId = null;
        if (includeNonOwnerTeacher)
        {
            var otherTeacher = new Teacher
            {
                Id = 20,
                UserId = 200,
                EmployeeNumber = "T-200",
                FirstName = "Grace",
                LastName = "Other",
                Department = "College"
            };

            context.Teachers.Add(otherTeacher);
            otherTeacherUserId = otherTeacher.UserId;
        }

        await context.SaveChangesAsync();

        return new SeededScheduleData(
            schedule.Id,
            section.Id,
            subject.Id,
            academicYear.Id,
            ownerTeacher.Id,
            ownerTeacher.UserId,
            otherTeacherUserId);
    }

    private sealed record SeededScheduleData(
        int ScheduleId,
        int SectionId,
        int SubjectId,
        int AcademicYearId,
        int OwnerTeacherId,
        int OwnerUserId,
        int? OtherTeacherUserId);
}
