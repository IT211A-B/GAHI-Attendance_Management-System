using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Sections");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(s => s.AcademicProgram)
            .WithMany(ap => ap.Sections)
            .HasForeignKey(s => s.AcademicProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.AcademicPeriod)
            .WithMany(ap => ap.Sections)
            .HasForeignKey(s => s.AcademicPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite unique: same section name can't repeat for same program + period
        builder.HasIndex(s => new { s.Name, s.AcademicProgramId, s.AcademicPeriodId }).IsUnique();
    }
}
