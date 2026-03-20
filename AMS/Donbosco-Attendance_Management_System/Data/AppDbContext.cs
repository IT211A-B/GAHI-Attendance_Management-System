using Microsoft.EntityFrameworkCore;
using Donbosco_Attendance_Management_System.Models;

namespace Donbosco_Attendance_Management_System.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<ScheduleStudent> ScheduleStudents { get; set; }
    public DbSet<Attendance> Attendances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Role).HasDefaultValue("teacher");
            entity.Property(u => u.IsActive).HasDefaultValue(true);
            entity.Property(u => u.CreatedAt).HasDefaultValueSql("now()");

            // Add CHECK constraint for role
            entity.ToTable(t => t.HasCheckConstraint("CK_users_role", "role IN ('admin', 'teacher')"));
        });

        // Configure Classroom entity
        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasIndex(c => c.RoomNumber).IsUnique();
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        });

        // Configure Section entity
        modelBuilder.Entity<Section>(entity =>
        {
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        });

        // Configure Student entity
        modelBuilder.Entity<Student>(entity =>
        {
            entity.Property(s => s.IsIrregular).HasDefaultValue(false);
            entity.Property(s => s.IsActive).HasDefaultValue(true);
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(s => s.Section)
                .WithMany(s => s.Students)
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Schedule entity
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

            // Add CHECK constraint for time_out > time_in
            entity.ToTable(t => t.HasCheckConstraint("CK_schedules_time", "time_out > time_in"));

            entity.HasOne(s => s.Section)
                .WithMany(s => s.Schedules)
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Teacher)
                .WithMany(u => u.Schedules)
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Classroom)
                .WithMany(c => c.Schedules)
                .HasForeignKey(s => s.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index for timetable queries
            entity.HasIndex(s => new { s.SectionId, s.DayOfWeek });
        });

        // Configure ScheduleStudent entity (composite key)
        modelBuilder.Entity<ScheduleStudent>(entity =>
        {
            entity.HasKey(ss => new { ss.ScheduleId, ss.StudentId });

            entity.HasOne(ss => ss.Schedule)
                .WithMany(s => s.ScheduleStudents)
                .HasForeignKey(ss => ss.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ss => ss.Student)
                .WithMany(s => s.ScheduleStudents)
                .HasForeignKey(ss => ss.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Attendance entity
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

            // Add CHECK constraint for status
            entity.ToTable(t => t.HasCheckConstraint("CK_attendance_status", "status IN ('present', 'absent', 'late')"));

            // Add UNIQUE constraint for (schedule_id, student_id, date)
            entity.HasIndex(a => new { a.ScheduleId, a.StudentId, a.Date }).IsUnique();

            // Add composite index for history queries
            entity.HasIndex(a => new { a.ScheduleId, a.Date });

            entity.HasOne(a => a.Schedule)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.MarkedByUser)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.MarkedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Fixed GUIDs for seed data
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var teacher1Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var teacher2Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var classroom1Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
        var classroom2Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
        var section1Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
        var section2Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
        var student1Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
        var student2Id = Guid.Parse("00000000-0000-0000-0000-000000000009");
        var student3Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var student4Id = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var student5Id = Guid.Parse("00000000-0000-0000-0000-000000000012");

        // Seed Users (1 admin, 2 teachers)
        // Pre-computed BCrypt hashes for deterministic seed data
        // admin123 -> $2a$11$arZS5F.8o8EpqHNOb6yF3ufkBTFuqKWnc9nclNZWZroGhK1Iu/XTS
        // teacher123 -> $2a$11$/SDELk6u7e/D02SNEfguSeEx5qcYFmuD876.Q2TRGCuT9.llP.BiC
        // teacher123 -> $2a$11$CAksnS39Lk/1zCd9UxDqHOHJ3qxPVYDmNnQcXPBgSdYkc6nBCWXxS
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminId,
                Name = "System Admin",
                Email = "admin@donbosco.edu",
                PasswordHash = "$2a$11$arZS5F.8o8EpqHNOb6yF3ufkBTFuqKWnc9nclNZWZroGhK1Iu/XTS",
                Role = "admin",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = teacher1Id,
                Name = "Mr. Juan Santos",
                Email = "jsantos@donbosco.edu",
                PasswordHash = "$2a$11$/SDELk6u7e/D02SNEfguSeEx5qcYFmuD876.Q2TRGCuT9.llP.BiC",
                Role = "teacher",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = teacher2Id,
                Name = "Ms. Maria Reyes",
                Email = "mreyes@donbosco.edu",
                PasswordHash = "$2a$11$CAksnS39Lk/1zCd9UxDqHOHJ3qxPVYDmNnQcXPBgSdYkc6nBCWXxS",
                Role = "teacher",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Classrooms (2)
        modelBuilder.Entity<Classroom>().HasData(
            new Classroom
            {
                Id = classroom1Id,
                Name = "Room 101",
                RoomNumber = "101",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Classroom
            {
                Id = classroom2Id,
                Name = "Room 102",
                RoomNumber = "102",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Sections (2)
        modelBuilder.Entity<Section>().HasData(
            new Section
            {
                Id = section1Id,
                Name = "Grade 7-A",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Section
            {
                Id = section2Id,
                Name = "Grade 7-B",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed Students (5)
        modelBuilder.Entity<Student>().HasData(
            new Student
            {
                Id = student1Id,
                Name = "Ana Garcia",
                SectionId = section1Id,
                IsIrregular = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Student
            {
                Id = student2Id,
                Name = "Ben Cruz",
                SectionId = section1Id,
                IsIrregular = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Student
            {
                Id = student3Id,
                Name = "Carla Mendoza",
                SectionId = section1Id,
                IsIrregular = true, // Irregular student
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Student
            {
                Id = student4Id,
                Name = "Diego Torres",
                SectionId = section2Id,
                IsIrregular = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Student
            {
                Id = student5Id,
                Name = "Elena Villanueva",
                SectionId = section2Id,
                IsIrregular = false,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}