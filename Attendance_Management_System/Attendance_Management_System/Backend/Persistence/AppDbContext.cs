using Attendance_Management_System.Backend.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Persistence;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Classroom> Classrooms { get; set; } = null!;
    public DbSet<AcademicYear> AcademicYears { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<Section> Sections { get; set; } = null!;
    public DbSet<Teacher> Teachers { get; set; } = null!;
    public DbSet<SectionTeacher> SectionTeachers { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Schedule> Schedules { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<AttendanceReport> AttendanceReports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Role)
                .IsRequired()
                .HasDefaultValue("student");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_User_Role",
                "\"Role\" IN ('admin', 'teacher', 'student')"));
        });

        // Classroom configuration
        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.Property(c => c.Name).IsRequired();
        });

        // AcademicYear configuration
        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.Property(a => a.YearLabel).IsRequired();
            entity.HasIndex(a => a.YearLabel).IsUnique();
        });

        // Course configuration
        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(c => c.Name).IsRequired();
            entity.Property(c => c.Code).IsRequired();
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.Code).IsUnique();
        });

        // Subject configuration
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.Property(s => s.Name).IsRequired();
            entity.Property(s => s.Code).IsRequired();
            entity.HasIndex(s => s.Code).IsUnique();

            entity.HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Section configuration
        modelBuilder.Entity<Section>(entity =>
        {
            entity.Property(s => s.Name).IsRequired();

            entity.HasOne(s => s.AcademicYear)
                .WithMany()
                .HasForeignKey(s => s.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Subject)
                .WithMany()
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Classroom)
                .WithMany()
                .HasForeignKey(s => s.ClassroomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Teacher configuration
        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).UseIdentityByDefaultColumn();
            entity.Property(t => t.EmployeeNumber).IsRequired();
            entity.Property(t => t.FirstName).IsRequired();
            entity.Property(t => t.LastName).IsRequired();
            entity.Property(t => t.Department).IsRequired();

            entity.HasIndex(t => t.UserId).IsUnique();
            entity.HasIndex(t => t.EmployeeNumber).IsUnique();

            entity.HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SectionTeacher (bridge table) configuration
        modelBuilder.Entity<SectionTeacher>(entity =>
        {
            entity.HasKey(st => new { st.SectionId, st.TeacherId });

            entity.HasOne(st => st.Section)
                .WithMany()
                .HasForeignKey(st => st.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(st => st.Teacher)
                .WithMany()
                .HasForeignKey(st => st.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Student configuration
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).UseIdentityByDefaultColumn();
            entity.Property(s => s.StudentNumber).IsRequired();
            entity.Property(s => s.FirstName).IsRequired();
            entity.Property(s => s.LastName).IsRequired();
            entity.Property(s => s.Address).IsRequired();
            entity.Property(s => s.GuardianName).IsRequired();
            entity.Property(s => s.GuardianContact).IsRequired();

            entity.HasIndex(s => s.StudentNumber).IsUnique();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Student_Gender",
                "\"Gender\" IN ('M', 'F', 'Other')"));

            entity.HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Section)
                .WithMany()
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Schedule configuration
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasOne(s => s.Section)
                .WithMany()
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Subject)
                .WithMany()
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Schedule_EndTime",
                "\"EndTime\" > \"StartTime\""));
        });

        // Attendance configuration
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).UseIdentityByDefaultColumn();

            entity.HasIndex(a => new { a.ScheduleId, a.StudentId, a.Date }).IsUnique();

            entity.HasOne(a => a.Schedule)
                .WithMany()
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AcademicYear)
                .WithMany()
                .HasForeignKey(a => a.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Section)
                .WithMany()
                .HasForeignKey(a => a.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Marker)
                .WithMany()
                .HasForeignKey(a => a.MarkedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Enrollment configuration
        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(e => e.Status).IsRequired().HasDefaultValue("pending");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Enrollment_Status",
                "\"Status\" IN ('pending', 'approved', 'rejected')"));

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Section)
                .WithMany()
                .HasForeignKey(e => e.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AcademicYear)
                .WithMany()
                .HasForeignKey(e => e.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AttendanceReport configuration
        modelBuilder.Entity<AttendanceReport>(entity =>
        {
            entity.Property(r => r.ReportType).IsRequired();
            entity.Property(r => r.DataJson).IsRequired();

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_AttendanceReport_ReportType",
                "\"ReportType\" IN ('daily', 'weekly', 'monthly', 'term')"));

            entity.HasOne(r => r.Section)
                .WithMany()
                .HasForeignKey(r => r.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.AcademicYear)
                .WithMany()
                .HasForeignKey(r => r.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Generator)
                .WithMany()
                .HasForeignKey(r => r.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}