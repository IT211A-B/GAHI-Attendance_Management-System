using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class BusinessRuleConfiguration : IEntityTypeConfiguration<BusinessRule>
{
    public void Configure(EntityTypeBuilder<BusinessRule> builder)
    {
        builder.ToTable("BusinessRules");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.RuleKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.RuleValue)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.Description)
            .HasMaxLength(500);

        // Composite unique: same rule key can only appear once per department (or once globally when DepartmentId is null)
        builder.HasIndex(b => new { b.RuleKey, b.DepartmentId }).IsUnique();

        builder.HasOne(b => b.Department)
            .WithMany(d => d.BusinessRules)
            .HasForeignKey(b => b.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
