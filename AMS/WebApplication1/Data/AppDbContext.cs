using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models.Entities;

namespace WebApplication1.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<ClassAttendance> ClassAttendances { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Subject relationships
        builder.Entity<Subject>()
            .HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Student relationships
        builder.Entity<Student>()
            .HasOne(s => s.Subject)
            .WithMany(sub => sub.Students)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Schedule relationships
        builder.Entity<Schedule>()
            .HasOne(s => s.Subject)
            .WithMany(sub => sub.Schedules)
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Schedule>()
            .HasOne(s => s.Room)
            .WithMany()
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // ClassAttendance relationships
        builder.Entity<ClassAttendance>()
            .HasOne(ca => ca.Schedule)
            .WithMany(s => s.Attendances)
            .HasForeignKey(ca => ca.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClassAttendance>()
            .HasOne(ca => ca.Student)
            .WithMany(s => s.Attendances)
            .HasForeignKey(ca => ca.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
