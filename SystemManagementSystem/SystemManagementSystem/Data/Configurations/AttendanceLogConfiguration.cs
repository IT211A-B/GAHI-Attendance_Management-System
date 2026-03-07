using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        builder.ToTable("AttendanceLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PersonType)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.ScanType)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(a => a.RawScanData)
            .HasMaxLength(500);

        builder.Property(a => a.Remarks)
            .HasMaxLength(500);

        // Indexes for fast querying
        builder.HasIndex(a => a.ScannedAt);
        builder.HasIndex(a => a.StudentId);
        builder.HasIndex(a => a.StaffId);

        builder.HasOne(a => a.Student)
            .WithMany(s => s.AttendanceLogs)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Staff)
            .WithMany(s => s.AttendanceLogs)
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.GateTerminal)
            .WithMany(g => g.AttendanceLogs)
            .HasForeignKey(a => a.GateTerminalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
