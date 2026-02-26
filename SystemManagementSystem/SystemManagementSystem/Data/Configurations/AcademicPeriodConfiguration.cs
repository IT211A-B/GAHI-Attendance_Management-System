using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class AcademicPeriodConfiguration : IEntityTypeConfiguration<AcademicPeriod>
{
    public void Configure(EntityTypeBuilder<AcademicPeriod> builder)
    {
        builder.ToTable("AcademicPeriods");

        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(ap => ap.Name).IsUnique();
    }
}
