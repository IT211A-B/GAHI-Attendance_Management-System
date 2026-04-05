using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

        // Check if data already seeded
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // Seed Admin User
        var adminUser = new User
        {
            UserName = "admin@school.edu",
            Email = "admin@school.edu",
            Role = "admin",
            IsActive = true,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await context.SaveChangesAsync();

        // Seed Teacher Users
        var teacher1User = new User
        {
            UserName = "teacher1@school.edu",
            Email = "teacher1@school.edu",
            Role = "teacher",
            IsActive = true,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(teacher1User, "Teacher123!");

        var teacher2User = new User
        {
            UserName = "teacher2@school.edu",
            Email = "teacher2@school.edu",
            Role = "teacher",
            IsActive = true,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(teacher2User, "Teacher123!");
        await context.SaveChangesAsync();

        // Seed Classrooms
        var classroom1 = new Classroom { Name = "Room 101", Description = "First floor classroom" };
        var classroom2 = new Classroom { Name = "Room 201", Description = "Second floor classroom" };
        context.Classrooms.AddRange(classroom1, classroom2);
        await context.SaveChangesAsync();

        // Seed Academic Year
        var academicYear = new AcademicYear
        {
            YearLabel = "2025-2026",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2026, 3, 31),
            IsActive = true
        };
        context.AcademicYears.Add(academicYear);
        await context.SaveChangesAsync();

        // Seed Courses
        var course1 = new Course { Name = "Bachelor of Science in Computer Science", Code = "BSCS", Description = "Computer Science Program" };
        var course2 = new Course { Name = "Bachelor of Science in Information Technology", Code = "BSIT", Description = "Information Technology Program" };
        context.Courses.AddRange(course1, course2);
        await context.SaveChangesAsync();

        // Seed Subjects
        var subject1 = new Subject { Name = "Data Structures and Algorithms", Code = "CS101", CourseId = course1.Id, Units = 3 };
        var subject2 = new Subject { Name = "Web Development", Code = "IT101", CourseId = course2.Id, Units = 3 };
        context.Subjects.AddRange(subject1, subject2);
        await context.SaveChangesAsync();

        // Seed Sections
        var section1 = new Section
        {
            Name = "Grade 7-A",
            AcademicYearId = academicYear.Id,
            CourseId = course1.Id,
            SubjectId = subject1.Id,
            ClassroomId = classroom1.Id
        };
        var section2 = new Section
        {
            Name = "Grade 7-B",
            AcademicYearId = academicYear.Id,
            CourseId = course2.Id,
            SubjectId = subject2.Id,
            ClassroomId = classroom2.Id
        };
        context.Sections.AddRange(section1, section2);
        await context.SaveChangesAsync();

        // Seed Teachers
        var teacher1 = new Teacher
        {
            UserId = teacher1User.Id,
            EmployeeNumber = "EMP001",
            FirstName = "John",
            LastName = "Smith",
            MiddleName = "Doe",
            Department = "Computer Science",
            Specialization = "Data Structures"
        };
        var teacher2 = new Teacher
        {
            UserId = teacher2User.Id,
            EmployeeNumber = "EMP002",
            FirstName = "Jane",
            LastName = "Doe",
            MiddleName = null,
            Department = "Information Technology",
            Specialization = "Web Development"
        };
        context.Teachers.AddRange(teacher1, teacher2);
        await context.SaveChangesAsync();

        // Seed Students (5 students)
        var studentUsers = new List<User>();
        var students = new List<Student>();

        for (int i = 1; i <= 5; i++)
        {
            var studentUser = new User
            {
                UserName = $"student{i}@school.edu",
                Email = $"student{i}@school.edu",
                Role = "student",
                IsActive = true,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(studentUser, "Student123!");
            studentUsers.Add(studentUser);
        }
        await context.SaveChangesAsync();

        var sectionIds = new[] { section1.Id, section2.Id };
        for (int i = 0; i < 5; i++)
        {
            var student = new Student
            {
                UserId = studentUsers[i].Id,
                CourseId = i % 2 == 0 ? course1.Id : course2.Id,
                SectionId = sectionIds[i % 2],
                StudentNumber = $"STU2025{i + 1:D3}",
                FirstName = $"Student{i + 1}",
                LastName = $"Last{i + 1}",
                MiddleName = i % 2 == 0 ? "M" : null,
                Birthdate = new DateOnly(2005, 1, i + 1),
                Gender = i % 2 == 0 ? "M" : "F",
                Address = $"Address {i + 1}, City",
                GuardianName = $"Guardian {i + 1}",
                GuardianContact = $"+12345678{i + 1:D2}",
                YearLevel = 1,
                IsActive = true
            };
            students.Add(student);
        }
        context.Students.AddRange(students);
        await context.SaveChangesAsync();

        // Seed SectionTeachers (assign teachers to sections)
        var sectionTeacher1 = new SectionTeacher
        {
            SectionId = section1.Id,
            TeacherId = teacher1.Id,
            AssignedAt = DateTimeOffset.UtcNow
        };
        var sectionTeacher2 = new SectionTeacher
        {
            SectionId = section2.Id,
            TeacherId = teacher2.Id,
            AssignedAt = DateTimeOffset.UtcNow
        };
        context.SectionTeachers.AddRange(sectionTeacher1, sectionTeacher2);
        await context.SaveChangesAsync();

        // Seed Schedules
        var schedule1 = new Schedule
        {
            SectionId = section1.Id,
            TeacherId = teacher1.Id,
            SubjectId = subject1.Id,
            DayOfWeek = 1, // Monday
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
            EffectiveFrom = new DateOnly(2025, 6, 1)
        };
        var schedule2 = new Schedule
        {
            SectionId = section1.Id,
            TeacherId = teacher1.Id,
            SubjectId = subject1.Id,
            DayOfWeek = 3, // Wednesday
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
            EffectiveFrom = new DateOnly(2025, 6, 1)
        };
        var schedule3 = new Schedule
        {
            SectionId = section1.Id,
            TeacherId = teacher1.Id,
            SubjectId = subject1.Id,
            DayOfWeek = 5, // Friday
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
            EffectiveFrom = new DateOnly(2025, 6, 1)
        };
        var schedule4 = new Schedule
        {
            SectionId = section2.Id,
            TeacherId = teacher2.Id,
            SubjectId = subject2.Id,
            DayOfWeek = 2, // Tuesday
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            EffectiveFrom = new DateOnly(2025, 6, 1)
        };
        var schedule5 = new Schedule
        {
            SectionId = section2.Id,
            TeacherId = teacher2.Id,
            SubjectId = subject2.Id,
            DayOfWeek = 4, // Thursday
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            EffectiveFrom = new DateOnly(2025, 6, 1)
        };
        context.Schedules.AddRange(schedule1, schedule2, schedule3, schedule4, schedule5);
        await context.SaveChangesAsync();
    }
}
