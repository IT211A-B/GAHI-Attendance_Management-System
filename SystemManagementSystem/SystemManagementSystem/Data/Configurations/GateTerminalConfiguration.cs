using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SystemManagementSystem.Models.Entities;

namespace SystemManagementSystem.Data.Configurations;

public class GateTerminalConfiguration : IEntityTypeConfiguration<GateTerminal>
{
    public void Configure(EntityTypeBuilder<GateTerminal> builder)
    {
        builder.ToTable("GateTerminals");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.TerminalType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(g => g.Name).IsUnique();
    }
}
