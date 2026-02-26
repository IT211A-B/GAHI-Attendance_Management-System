using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .HasMaxLength(50);

        // JSON columns for old/new values
        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        builder.HasIndex(a => a.PerformedAt);
        builder.HasIndex(a => a.EntityName);

        builder.HasOne(a => a.PerformedByUser)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
