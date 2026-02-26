using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentIdNumber)
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

        builder.Property(s => s.EnrollmentStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.StudentIdNumber).IsUnique();
        builder.HasIndex(s => s.QrCodeData).IsUnique().HasFilter("\"QrCodeData\" IS NOT NULL");

        builder.HasOne(s => s.Section)
            .WithMany(sec => sec.Students)
            .HasForeignKey(s => s.SectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
