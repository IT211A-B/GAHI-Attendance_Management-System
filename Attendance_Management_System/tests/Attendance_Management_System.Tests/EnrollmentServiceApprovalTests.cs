using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Attendance_Management_System.Tests;

public class EnrollmentServiceApprovalTests
{
    [Fact]
    public async Task UpdateEnrollmentStatusAsync_Approved_ConfirmsStudentUserAndAssignsSection()
    {
        await using var context = CreateContext();
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock
            .Setup(service => service.CreateAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new Notification());

        var accountEmailServiceMock = new Mock<IAccountEmailService>();
        accountEmailServiceMock
            .Setup(service => service.SendEnrollmentStatusUpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var sectionAllocationServiceMock = new Mock<ISectionAllocationService>(MockBehavior.Strict);
        var service = new EnrollmentService(
            context,
            Options.Create(EnrollmentSettings.Default),
            sectionAllocationServiceMock.Object,
            notificationServiceMock.Object,
            accountEmailServiceMock.Object,
            NullLogger<EnrollmentService>.Instance);

        var admin = new User
        {
            Id = 101,
            UserName = "admin@example.com",
            Email = "admin@example.com",
            Role = "admin",
            IsActive = true,
            EmailConfirmed = true
        };

        var studentUser = new User
        {
            Id = 202,
            UserName = "student@example.com",
            Email = "student@example.com",
            Role = "student",
            IsActive = false,
            EmailConfirmed = false
        };

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
            Id = 1,
            Name = "BSIT",
            Code = "BSIT",
            EducationLevel = EducationLevel.College
        };

        var subject = new Subject
        {
            Id = 1,
            Name = "Programming 1",
            Code = "CS101",
            CourseId = course.Id
        };

        var classroom = new Classroom
        {
            Id = 1,
            Name = "Lab 1",
            Description = "Main building"
        };

        var section = new Section
        {
            Id = 1,
            Name = "BSIT-1A",
            YearLevel = 1,
            AcademicYearId = academicYear.Id,
            CourseId = course.Id,
            SubjectId = subject.Id,
            ClassroomId = classroom.Id
        };

        var student = new Student
        {
            Id = 1,
            UserId = studentUser.Id,
            CourseId = course.Id,
            SectionId = null,
            StudentNumber = "2026-0001",
            FirstName = "Jake",
            LastName = "Sucgang",
            Birthdate = new DateOnly(2007, 1, 1),
            Gender = "M",
            Address = "Cebu",
            GuardianName = "Parent",
            GuardianContact = "09123456789",
            YearLevel = 1,
            IsActive = true
        };

        var enrollment = new Enrollment
        {
            Id = 1,
            StudentId = student.Id,
            SectionId = section.Id,
            AcademicYearId = academicYear.Id,
            Status = "pending"
        };

        context.Users.AddRange(admin, studentUser);
        context.AcademicYears.Add(academicYear);
        context.Courses.Add(course);
        context.Subjects.Add(subject);
        context.Classrooms.Add(classroom);
        context.Sections.Add(section);
        context.Students.Add(student);
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var result = await service.UpdateEnrollmentStatusAsync(
            enrollment.Id,
            new UpdateEnrollmentStatusRequest { Status = "approved" },
            admin.Id);

        Assert.True(result.Success);

        var updatedEnrollment = await context.Enrollments.AsNoTracking().SingleAsync(item => item.Id == enrollment.Id);
        var updatedStudent = await context.Students.AsNoTracking().SingleAsync(item => item.Id == student.Id);
        var updatedUser = await context.Users.AsNoTracking().SingleAsync(item => item.Id == studentUser.Id);

        Assert.Equal("approved", updatedEnrollment.Status);
        Assert.Equal(section.Id, updatedStudent.SectionId);
        Assert.True(updatedUser.IsActive);
        Assert.True(updatedUser.EmailConfirmed);
    }

    [Fact]
    public async Task UpdateEnrollmentStatusAsync_Rejected_KeepsEmailBoundAndUnlocksSignIn()
    {
        await using var context = CreateContext();
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock
            .Setup(service => service.CreateAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new Notification());

        var accountEmailServiceMock = new Mock<IAccountEmailService>();
        accountEmailServiceMock
            .Setup(service => service.SendEnrollmentStatusUpdateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var sectionAllocationServiceMock = new Mock<ISectionAllocationService>(MockBehavior.Strict);
        var service = new EnrollmentService(
            context,
            Options.Create(EnrollmentSettings.Default),
            sectionAllocationServiceMock.Object,
            notificationServiceMock.Object,
            accountEmailServiceMock.Object,
            NullLogger<EnrollmentService>.Instance);

        var admin = new User
        {
            Id = 301,
            UserName = "admin@example.com",
            Email = "admin@example.com",
            Role = "admin",
            IsActive = true,
            EmailConfirmed = true
        };

        var studentUser = new User
        {
            Id = 302,
            UserName = "student2@example.com",
            Email = "student2@example.com",
            Role = "student",
            IsActive = false,
            EmailConfirmed = false
        };

        var academicYear = new AcademicYear
        {
            Id = 2,
            YearLabel = "2026-2027",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2027, 3, 31),
            IsActive = true
        };

        var course = new Course
        {
            Id = 2,
            Name = "BSCS",
            Code = "BSCS",
            EducationLevel = EducationLevel.College
        };

        var subject = new Subject
        {
            Id = 2,
            Name = "Discrete Math",
            Code = "MATH201",
            CourseId = course.Id
        };

        var classroom = new Classroom
        {
            Id = 2,
            Name = "Room 2",
            Description = "Annex"
        };

        var section = new Section
        {
            Id = 2,
            Name = "BSCS-1A",
            YearLevel = 1,
            AcademicYearId = academicYear.Id,
            CourseId = course.Id,
            SubjectId = subject.Id,
            ClassroomId = classroom.Id
        };

        var student = new Student
        {
            Id = 2,
            UserId = studentUser.Id,
            CourseId = course.Id,
            SectionId = null,
            StudentNumber = "2026-0002",
            FirstName = "Alex",
            LastName = "Reyes",
            Birthdate = new DateOnly(2007, 2, 2),
            Gender = "F",
            Address = "Cebu",
            GuardianName = "Parent",
            GuardianContact = "09121212121",
            YearLevel = 1,
            IsActive = true
        };

        var enrollment = new Enrollment
        {
            Id = 2,
            StudentId = student.Id,
            SectionId = section.Id,
            AcademicYearId = academicYear.Id,
            Status = "pending"
        };

        context.Users.AddRange(admin, studentUser);
        context.AcademicYears.Add(academicYear);
        context.Courses.Add(course);
        context.Subjects.Add(subject);
        context.Classrooms.Add(classroom);
        context.Sections.Add(section);
        context.Students.Add(student);
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var result = await service.UpdateEnrollmentStatusAsync(
            enrollment.Id,
            new UpdateEnrollmentStatusRequest
            {
                Status = "rejected",
                RejectionReason = "Missing admission requirements."
            },
            admin.Id);

        Assert.True(result.Success);

        var updatedEnrollment = await context.Enrollments.AsNoTracking().SingleAsync(item => item.Id == enrollment.Id);
        var updatedStudent = await context.Students.AsNoTracking().SingleAsync(item => item.Id == student.Id);
        var updatedUser = await context.Users.AsNoTracking().SingleAsync(item => item.Id == studentUser.Id);

        Assert.Equal("rejected", updatedEnrollment.Status);
        Assert.Equal("Missing admission requirements.", updatedEnrollment.RejectionReason);
        Assert.Null(updatedStudent.SectionId);
        Assert.True(updatedUser.IsActive);
        Assert.True(updatedUser.EmailConfirmed);
    }

    [Fact]
    public async Task CreateEnrollmentAsync_ReturnsBadRequest_WhenYearLevelOutsideCourseRange()
    {
        await using var context = CreateContext();
        var notificationServiceMock = new Mock<INotificationService>(MockBehavior.Strict);
        var accountEmailServiceMock = new Mock<IAccountEmailService>(MockBehavior.Strict);
        var sectionAllocationServiceMock = new Mock<ISectionAllocationService>(MockBehavior.Strict);

        var service = new EnrollmentService(
            context,
            Options.Create(EnrollmentSettings.Default),
            sectionAllocationServiceMock.Object,
            notificationServiceMock.Object,
            accountEmailServiceMock.Object,
            NullLogger<EnrollmentService>.Instance);

        var course = new Course
        {
            Id = 500,
            Name = "Diploma in Electrical Technology",
            Code = "DET",
            EducationLevel = EducationLevel.Tvet
        };

        var student = new Student
        {
            Id = 900,
            UserId = 901,
            StudentNumber = "2026-9001",
            FirstName = "Maria",
            LastName = "Santos",
            Birthdate = new DateOnly(2009, 2, 2),
            Gender = "F",
            Address = "Cebu",
            GuardianName = "Parent",
            GuardianContact = "09123456789",
            YearLevel = 1,
            CourseId = course.Id,
            IsActive = true
        };

        context.Courses.Add(course);
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var response = await service.CreateEnrollmentAsync(
            new CreateEnrollmentRequest
            {
                CourseId = course.Id,
                AcademicYearId = 777,
                YearLevel = 4
            },
            student.UserId);

        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Equal(ErrorCodes.BadRequest, response.Error!.Code);
        Assert.Equal("Year level 4 is not valid for TVET. Allowed range is 1-2.", response.Error.Message);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
