using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
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

        async Task<User> CreateUserOrThrowAsync(string email, UserRole role, string password)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                Role = role.ToStorageValue(),
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
                throw new InvalidOperationException($"Failed to create seed user '{email}'. {errors}");
            }

            return user;
        }

        // Seed admin account
        await CreateUserOrThrowAsync("admin@dbtc-cebu.edu.ph", UserRole.Admin, "Admin123!");

        // Seed teacher accounts and profiles
        var teacherSeeds = new[]
        {
            (Email: "it.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T001", FirstName: "Mark", LastName: "Villanueva", MiddleName: (string?)"Santos", Department: "College - Information Technology", Specialization: "Software Development"),
            (Email: "me.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T002", FirstName: "Carlo", LastName: "Reyes", MiddleName: (string?)"Mendoza", Department: "College - Mechanical Engineering", Specialization: "Thermodynamics and CAD"),
            (Email: "tvted.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T003", FirstName: "Maria", LastName: "Gonzales", MiddleName: (string?)"Lopez", Department: "College - Technical-Vocational Teacher Education", Specialization: "Instructional Design"),
            (Email: "repc.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T004", FirstName: "Jose", LastName: "Abella", MiddleName: null, Department: "College - Religious Education", Specialization: "Pastoral Communication"),
            (Email: "jhs.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T005", FirstName: "Adrian", LastName: "Lim", MiddleName: (string?)"Tan", Department: "Basic Education - Junior High School", Specialization: "Mathematics"),
            (Email: "shs.abm.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T006", FirstName: "Clarisse", LastName: "Dizon", MiddleName: null, Department: "Basic Education - Senior High School", Specialization: "ABM"),
            (Email: "shs.humss.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T007", FirstName: "Noel", LastName: "Fernandez", MiddleName: null, Department: "Basic Education - Senior High School", Specialization: "HUMSS"),
            (Email: "shs.stem.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T008", FirstName: "Vincent", LastName: "Torres", MiddleName: null, Department: "Basic Education - Senior High School", Specialization: "STEM"),
            (Email: "tvet.mech.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T009", FirstName: "Roberto", LastName: "Sarmiento", MiddleName: null, Department: "TVET", Specialization: "Mechanical and Machining"),
            (Email: "tvet.elec.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T010", FirstName: "Elena", LastName: "Cabrera", MiddleName: null, Department: "TVET", Specialization: "Electrical Installation"),
            (Email: "tvet.furniture.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T011", FirstName: "Ricardo", LastName: "Dela Cruz", MiddleName: null, Department: "TVET", Specialization: "Furniture Making"),
            (Email: "elem.faculty@dbtc-cebu.edu.ph", EmployeeNumber: "DBTC-T012", FirstName: "Maricel", LastName: "Bautista", MiddleName: null, Department: "Basic Education - Elementary", Specialization: "Elementary Mathematics"),
        };

        var teacherUsersByEmployeeNumber = new Dictionary<string, User>();
        foreach (var teacherSeed in teacherSeeds)
        {
            teacherUsersByEmployeeNumber[teacherSeed.EmployeeNumber] =
                await CreateUserOrThrowAsync(teacherSeed.Email, UserRole.Teacher, "Teacher123!");
        }

        var teachers = teacherSeeds
            .Select(teacherSeed => new Teacher
            {
                UserId = teacherUsersByEmployeeNumber[teacherSeed.EmployeeNumber].Id,
                EmployeeNumber = teacherSeed.EmployeeNumber,
                FirstName = teacherSeed.FirstName,
                LastName = teacherSeed.LastName,
                MiddleName = teacherSeed.MiddleName,
                Department = teacherSeed.Department,
                Specialization = teacherSeed.Specialization,
                IsActive = true
            })
            .ToList();
        context.Teachers.AddRange(teachers);
        await context.SaveChangesAsync();
        var teachersByEmployeeNumber = teachers.ToDictionary(teacher => teacher.EmployeeNumber);

        // Seed classrooms used across college, K-12, and TVET tracks
        var classrooms = new[]
        {
            new Classroom { Name = "College IT Laboratory", Description = "Computer laboratory for BSIT classes" },
            new Classroom { Name = "College Mechanical Workshop", Description = "Workshop for engineering and machine classes" },
            new Classroom { Name = "College Lecture Hall A", Description = "Lecture hall for general college subjects" },
            new Classroom { Name = "Junior High Room 9-A", Description = "Room for Grade 9 core classes" },
            new Classroom { Name = "Senior High Room ABM", Description = "Senior High classroom for ABM track" },
            new Classroom { Name = "Senior High Room HUMSS", Description = "Senior High classroom for HUMSS track" },
            new Classroom { Name = "Senior High STEM Laboratory", Description = "Senior High STEM integrated laboratory" },
            new Classroom { Name = "Elementary Room 6-A", Description = "Elementary classroom for Grade 6" },
            new Classroom { Name = "TVET Mechanical Shop", Description = "Hands-on mechanical and machining training area" },
            new Classroom { Name = "TVET Electrical Lab", Description = "Electrical technology and installation training lab" },
            new Classroom { Name = "TVET Furniture Workshop", Description = "Furniture production and finishing workshop" },
            new Classroom { Name = "TVET Multi-Skills Center", Description = "Shared venue for NC-based practical sessions" }
        };
        context.Classrooms.AddRange(classrooms);
        await context.SaveChangesAsync();
        var classroomsByName = classrooms.ToDictionary(classroom => classroom.Name);

        // Seed active academic year
        var academicYear = new AcademicYear
        {
            YearLabel = "2026-2027",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2027, 3, 31),
            IsActive = true
        };
        context.AcademicYears.Add(academicYear);
        await context.SaveChangesAsync();

        // Programs sourced from DBTC-Cebu academics page (College, K-12, TVET)
        var courseSeeds = new[]
        {
            (Name: "Bachelor of Science in Information Technology", Code: "BSIT", Description: "College program", EducationLevel: EducationLevel.College),
            (Name: "Bachelor of Science in Mechanical Engineering", Code: "BSME", Description: "College program", EducationLevel: EducationLevel.College),
            (Name: "Bachelor of Technical-Vocational Teacher Education", Code: "BTVTED", Description: "College program", EducationLevel: EducationLevel.College),
            (Name: "Bachelor of Arts in Religious Education and Pastoral Communication", Code: "ABREPC", Description: "College program", EducationLevel: EducationLevel.College),

            (Name: "Elementary School (Grade 1 to Grade 6)", Code: "ES", Description: "K-12 basic education program", EducationLevel: EducationLevel.Elementary),

            (Name: "Junior High School (Grade 7 to Grade 10)", Code: "JHS", Description: "K-12 basic education program", EducationLevel: EducationLevel.JuniorHigh),
            (Name: "Senior High School - ABM", Code: "SHSABM", Description: "K-12 senior high strand", EducationLevel: EducationLevel.SeniorHigh),
            (Name: "Senior High School - HUMSS", Code: "SHSHUMSS", Description: "K-12 senior high strand", EducationLevel: EducationLevel.SeniorHigh),
            (Name: "Senior High School - STEM (Pre-Medical)", Code: "SHSSTPM", Description: "K-12 senior high strand", EducationLevel: EducationLevel.SeniorHigh),
            (Name: "Senior High School - STEM (Engineering)", Code: "SHSSTENG", Description: "K-12 senior high strand", EducationLevel: EducationLevel.SeniorHigh),
            (Name: "Senior High School - STEM (Information, Communication and Technology)", Code: "SHSSTICT", Description: "K-12 senior high strand", EducationLevel: EducationLevel.SeniorHigh),

            (Name: "Diploma in Mechanical Technology", Code: "DMT", Description: "TVET diploma program", EducationLevel: EducationLevel.Tvet),
            (Name: "Diploma in Electrical Technology", Code: "DET", Description: "TVET diploma program", EducationLevel: EducationLevel.Tvet),
            (Name: "Diploma in Furniture Making Technology", Code: "DFMT", Description: "TVET diploma program", EducationLevel: EducationLevel.Tvet),
            (Name: "Certificate in Furniture Making", Code: "CFM", Description: "TVET national certificate program", EducationLevel: EducationLevel.Tvet),
            (Name: "Certificate in Machining", Code: "CMACH", Description: "TVET national certificate program", EducationLevel: EducationLevel.Tvet),
            (Name: "Certificate in Motorcycle Servicing", Code: "CMS", Description: "TVET national certificate program", EducationLevel: EducationLevel.Tvet),
            (Name: "Certificate in Pharmacy Services", Code: "CPS", Description: "TVET national certificate program", EducationLevel: EducationLevel.Tvet)
        };

        var courses = courseSeeds
            .Select(courseSeed => new Course
            {
                Name = courseSeed.Name,
                Code = courseSeed.Code,
                Description = courseSeed.Description,
                EducationLevel = courseSeed.EducationLevel
            })
            .ToList();
        context.Courses.AddRange(courses);
        await context.SaveChangesAsync();
        var coursesByCode = courses.ToDictionary(course => course.Code);

        var subjectSeeds = new[]
        {
            (Name: "Programming Fundamentals", Code: "IT101", CourseCode: "BSIT", Units: 3),
            (Name: "Engineering Drawing and CAD", Code: "ME101", CourseCode: "BSME", Units: 3),
            (Name: "Foundations of Technical-Vocational Education", Code: "TVTED101", CourseCode: "BTVTED", Units: 3),
            (Name: "Religious Education and Pastoral Communication Basics", Code: "REPC101", CourseCode: "ABREPC", Units: 3),

            (Name: "Elementary Mathematics 6", Code: "ELMATH6", CourseCode: "ES", Units: 3),

            (Name: "Mathematics 9", Code: "JHSMATH9", CourseCode: "JHS", Units: 3),
            (Name: "Fundamentals of Accountancy, Business and Management 1", Code: "ABM101", CourseCode: "SHSABM", Units: 3),
            (Name: "Introduction to World Religions and Belief Systems", Code: "HUMSS101", CourseCode: "SHSHUMSS", Units: 3),
            (Name: "General Biology 1 (Pre-Medical Track)", Code: "STEMPM101", CourseCode: "SHSSTPM", Units: 3),
            (Name: "Pre-Calculus for Engineering", Code: "STEMENG101", CourseCode: "SHSSTENG", Units: 3),
            (Name: "Computer Programming 1 (ICT Focus)", Code: "STEMICT101", CourseCode: "SHSSTICT", Units: 3),

            (Name: "Machine Shop Theory and Practice", Code: "DMT101", CourseCode: "DMT", Units: 4),
            (Name: "Electrical Installation and Maintenance Fundamentals", Code: "DET101", CourseCode: "DET", Units: 4),
            (Name: "Furniture Design and Production", Code: "DFMT101", CourseCode: "DFMT", Units: 4),
            (Name: "Furniture Making NC Core Skills", Code: "CFMNC101", CourseCode: "CFM", Units: 4),
            (Name: "Machining NC Core Skills", Code: "CMACHNC101", CourseCode: "CMACH", Units: 4),
            (Name: "Motorcycle Servicing NC Core Skills", Code: "CMSNC101", CourseCode: "CMS", Units: 4),
            (Name: "Pharmacy Services NC Core Skills", Code: "CPSNC101", CourseCode: "CPS", Units: 4)
        };

        var subjects = subjectSeeds
            .Select(subjectSeed => new Subject
            {
                Name = subjectSeed.Name,
                Code = subjectSeed.Code,
                CourseId = coursesByCode[subjectSeed.CourseCode].Id,
                Units = subjectSeed.Units
            })
            .ToList();
        context.Subjects.AddRange(subjects);
        await context.SaveChangesAsync();
        var subjectsByCode = subjects.ToDictionary(subject => subject.Code);

        var sectionSeeds = new[]
        {
            (Name: "COL-BSIT-1A", YearLevel: 1, CourseCode: "BSIT", SubjectCode: "IT101", Classroom: "College IT Laboratory", AdviserEmployeeNumber: "DBTC-T001"),
            (Name: "COL-BSIT-1B", YearLevel: 1, CourseCode: "BSIT", SubjectCode: "IT101", Classroom: "College IT Laboratory", AdviserEmployeeNumber: "DBTC-T001"),
            (Name: "COL-BSME-1A", YearLevel: 1, CourseCode: "BSME", SubjectCode: "ME101", Classroom: "College Mechanical Workshop", AdviserEmployeeNumber: "DBTC-T002"),
            (Name: "COL-BSME-1B", YearLevel: 1, CourseCode: "BSME", SubjectCode: "ME101", Classroom: "College Mechanical Workshop", AdviserEmployeeNumber: "DBTC-T002"),
            (Name: "COL-BTVTED-1A", YearLevel: 1, CourseCode: "BTVTED", SubjectCode: "TVTED101", Classroom: "College Lecture Hall A", AdviserEmployeeNumber: "DBTC-T003"),
            (Name: "COL-BTVTED-1B", YearLevel: 1, CourseCode: "BTVTED", SubjectCode: "TVTED101", Classroom: "College Lecture Hall A", AdviserEmployeeNumber: "DBTC-T003"),
            (Name: "COL-ABREPC-1A", YearLevel: 1, CourseCode: "ABREPC", SubjectCode: "REPC101", Classroom: "College Lecture Hall A", AdviserEmployeeNumber: "DBTC-T004"),
            (Name: "COL-ABREPC-1B", YearLevel: 1, CourseCode: "ABREPC", SubjectCode: "REPC101", Classroom: "College Lecture Hall A", AdviserEmployeeNumber: "DBTC-T004"),

            (Name: "ES-G6-A", YearLevel: 6, CourseCode: "ES", SubjectCode: "ELMATH6", Classroom: "Elementary Room 6-A", AdviserEmployeeNumber: "DBTC-T012"),
            (Name: "ES-G6-B", YearLevel: 6, CourseCode: "ES", SubjectCode: "ELMATH6", Classroom: "Elementary Room 6-A", AdviserEmployeeNumber: "DBTC-T012"),

            (Name: "JHS-G9-A", YearLevel: 9, CourseCode: "JHS", SubjectCode: "JHSMATH9", Classroom: "Junior High Room 9-A", AdviserEmployeeNumber: "DBTC-T005"),
            (Name: "JHS-G9-B", YearLevel: 9, CourseCode: "JHS", SubjectCode: "JHSMATH9", Classroom: "Junior High Room 9-A", AdviserEmployeeNumber: "DBTC-T005"),
            (Name: "SHS-ABM-11A", YearLevel: 11, CourseCode: "SHSABM", SubjectCode: "ABM101", Classroom: "Senior High Room ABM", AdviserEmployeeNumber: "DBTC-T006"),
            (Name: "SHS-ABM-11B", YearLevel: 11, CourseCode: "SHSABM", SubjectCode: "ABM101", Classroom: "Senior High Room ABM", AdviserEmployeeNumber: "DBTC-T006"),
            (Name: "SHS-HUMSS-11A", YearLevel: 11, CourseCode: "SHSHUMSS", SubjectCode: "HUMSS101", Classroom: "Senior High Room HUMSS", AdviserEmployeeNumber: "DBTC-T007"),
            (Name: "SHS-HUMSS-11B", YearLevel: 11, CourseCode: "SHSHUMSS", SubjectCode: "HUMSS101", Classroom: "Senior High Room HUMSS", AdviserEmployeeNumber: "DBTC-T007"),
            (Name: "SHS-STEM-PM-11A", YearLevel: 11, CourseCode: "SHSSTPM", SubjectCode: "STEMPM101", Classroom: "Senior High STEM Laboratory", AdviserEmployeeNumber: "DBTC-T008"),
            (Name: "SHS-STEM-PM-11B", YearLevel: 11, CourseCode: "SHSSTPM", SubjectCode: "STEMPM101", Classroom: "Senior High STEM Laboratory", AdviserEmployeeNumber: "DBTC-T008"),
            (Name: "SHS-STEM-ENG-11A", YearLevel: 11, CourseCode: "SHSSTENG", SubjectCode: "STEMENG101", Classroom: "Senior High STEM Laboratory", AdviserEmployeeNumber: "DBTC-T008"),
            (Name: "SHS-STEM-ENG-11B", YearLevel: 11, CourseCode: "SHSSTENG", SubjectCode: "STEMENG101", Classroom: "Senior High STEM Laboratory", AdviserEmployeeNumber: "DBTC-T008"),
            (Name: "SHS-STEM-ICT-11A", YearLevel: 11, CourseCode: "SHSSTICT", SubjectCode: "STEMICT101", Classroom: "Senior High STEM Laboratory", AdviserEmployeeNumber: "DBTC-T008"),
            (Name: "SHS-STEM-ICT-11B", YearLevel: 11, CourseCode: "SHSSTICT", SubjectCode: "STEMICT101", Classroom: "Senior High STEM Laboratory", AdviserEmployeeNumber: "DBTC-T008"),

            (Name: "TVET-DMT-B1", YearLevel: 1, CourseCode: "DMT", SubjectCode: "DMT101", Classroom: "TVET Mechanical Shop", AdviserEmployeeNumber: "DBTC-T009"),
            (Name: "TVET-DMT-B2", YearLevel: 1, CourseCode: "DMT", SubjectCode: "DMT101", Classroom: "TVET Mechanical Shop", AdviserEmployeeNumber: "DBTC-T009"),
            (Name: "TVET-DET-B1", YearLevel: 1, CourseCode: "DET", SubjectCode: "DET101", Classroom: "TVET Electrical Lab", AdviserEmployeeNumber: "DBTC-T010"),
            (Name: "TVET-DET-B2", YearLevel: 1, CourseCode: "DET", SubjectCode: "DET101", Classroom: "TVET Electrical Lab", AdviserEmployeeNumber: "DBTC-T010"),
            (Name: "TVET-DFMT-B1", YearLevel: 1, CourseCode: "DFMT", SubjectCode: "DFMT101", Classroom: "TVET Furniture Workshop", AdviserEmployeeNumber: "DBTC-T011"),
            (Name: "TVET-DFMT-B2", YearLevel: 1, CourseCode: "DFMT", SubjectCode: "DFMT101", Classroom: "TVET Furniture Workshop", AdviserEmployeeNumber: "DBTC-T011"),
            (Name: "TVET-CFM-NCII-B1", YearLevel: 1, CourseCode: "CFM", SubjectCode: "CFMNC101", Classroom: "TVET Furniture Workshop", AdviserEmployeeNumber: "DBTC-T011"),
            (Name: "TVET-CFM-NCII-B2", YearLevel: 1, CourseCode: "CFM", SubjectCode: "CFMNC101", Classroom: "TVET Furniture Workshop", AdviserEmployeeNumber: "DBTC-T011"),
            (Name: "TVET-CMACH-NCII-B1", YearLevel: 1, CourseCode: "CMACH", SubjectCode: "CMACHNC101", Classroom: "TVET Mechanical Shop", AdviserEmployeeNumber: "DBTC-T009"),
            (Name: "TVET-CMACH-NCII-B2", YearLevel: 1, CourseCode: "CMACH", SubjectCode: "CMACHNC101", Classroom: "TVET Mechanical Shop", AdviserEmployeeNumber: "DBTC-T009"),
            (Name: "TVET-CMS-NCII-B1", YearLevel: 1, CourseCode: "CMS", SubjectCode: "CMSNC101", Classroom: "TVET Mechanical Shop", AdviserEmployeeNumber: "DBTC-T009"),
            (Name: "TVET-CMS-NCII-B2", YearLevel: 1, CourseCode: "CMS", SubjectCode: "CMSNC101", Classroom: "TVET Mechanical Shop", AdviserEmployeeNumber: "DBTC-T009"),
            (Name: "TVET-CPS-NCIII-B1", YearLevel: 1, CourseCode: "CPS", SubjectCode: "CPSNC101", Classroom: "TVET Multi-Skills Center", AdviserEmployeeNumber: "DBTC-T010"),
            (Name: "TVET-CPS-NCIII-B2", YearLevel: 1, CourseCode: "CPS", SubjectCode: "CPSNC101", Classroom: "TVET Multi-Skills Center", AdviserEmployeeNumber: "DBTC-T010")
        };

        var sections = sectionSeeds
            .Select(sectionSeed => new Section
            {
                Name = sectionSeed.Name,
                YearLevel = sectionSeed.YearLevel,
                AcademicYearId = academicYear.Id,
                CourseId = coursesByCode[sectionSeed.CourseCode].Id,
                SubjectId = subjectsByCode[sectionSeed.SubjectCode].Id,
                ClassroomId = classroomsByName[sectionSeed.Classroom].Id
            })
            .ToList();
        context.Sections.AddRange(sections);
        await context.SaveChangesAsync();
        var sectionsByName = sections.ToDictionary(section => section.Name);
        var sectionSeedByName = sectionSeeds.ToDictionary(sectionSeed => sectionSeed.Name);

        var sectionTeachers = sectionSeeds
            .Select(sectionSeed => new SectionTeacher
            {
                SectionId = sectionsByName[sectionSeed.Name].Id,
                TeacherId = teachersByEmployeeNumber[sectionSeed.AdviserEmployeeNumber].Id,
                AssignedAt = DateTimeOffset.UtcNow
            })
            .ToList();
        context.SectionTeachers.AddRange(sectionTeachers);
        await context.SaveChangesAsync();

        // Build one base schedule per section while rotating each teacher's load by day/time.
        var dayPattern = new[] { 1, 2, 3, 4, 5 };
        var startPattern = new[]
        {
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            new TimeOnly(13, 0),
            new TimeOnly(15, 0)
        };
        var scheduleLoadByTeacher = new Dictionary<int, int>();
        var schedules = new List<Schedule>();

        foreach (var sectionSeed in sectionSeeds)
        {
            var teacherId = teachersByEmployeeNumber[sectionSeed.AdviserEmployeeNumber].Id;
            var loadIndex = scheduleLoadByTeacher.TryGetValue(teacherId, out var existingLoad) ? existingLoad : 0;
            scheduleLoadByTeacher[teacherId] = loadIndex + 1;

            var startTime = startPattern[(loadIndex / dayPattern.Length) % startPattern.Length];
            schedules.Add(new Schedule
            {
                SectionId = sectionsByName[sectionSeed.Name].Id,
                TeacherId = teacherId,
                SubjectId = subjectsByCode[sectionSeed.SubjectCode].Id,
                DayOfWeek = dayPattern[loadIndex % dayPattern.Length],
                StartTime = startTime,
                EndTime = startTime.AddHours(2),
                EffectiveFrom = academicYear.StartDate
            });
        }

        context.Schedules.AddRange(schedules);
        await context.SaveChangesAsync();

    }
}
