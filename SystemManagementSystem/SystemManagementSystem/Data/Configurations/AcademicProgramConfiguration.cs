using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("AcademicPrograms");

        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ap => ap.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ap => ap.Description)
            .HasMaxLength(500);

        builder.HasIndex(ap => ap.Code).IsUnique();

        builder.HasOne(ap => ap.Department)
            .WithMany(d => d.AcademicPrograms)
            .HasForeignKey(ap => ap.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
