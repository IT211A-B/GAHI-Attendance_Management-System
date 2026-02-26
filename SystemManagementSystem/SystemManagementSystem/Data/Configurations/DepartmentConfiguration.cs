using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.HasIndex(d => d.Code).IsUnique();
    }
}
