using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EmployeeIdNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.MiddleName)
            .HasMaxLength(100);

        builder.Property(s => s.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Email)
            .HasMaxLength(255);

        builder.Property(s => s.ContactNumber)
            .HasMaxLength(20);

        builder.Property(s => s.QrCodeData)
            .HasMaxLength(255);

        builder.Property(s => s.StaffType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.EmployeeIdNumber).IsUnique();
        builder.HasIndex(s => s.QrCodeData).IsUnique().HasFilter("\"QrCodeData\" IS NOT NULL");

        builder.HasOne(s => s.Department)
            .WithMany(d => d.Staff)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
