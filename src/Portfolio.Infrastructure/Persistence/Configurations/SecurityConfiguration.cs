using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class SecurityConfiguration : IEntityTypeConfiguration<Security>
{
    public void Configure(EntityTypeBuilder<Security> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Sector).HasMaxLength(100);
        builder.Property(s => s.Industry).HasMaxLength(100);

        builder.HasIndex(s => s.Symbol).IsUnique();

        builder.HasMany(s => s.DailyPrices)
            .WithOne(dp => dp.Security)
            .HasForeignKey(dp => dp.SecurityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Transactions)
            .WithOne(t => t.Security)
            .HasForeignKey(t => t.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lots)
            .WithOne(l => l.Security)
            .HasForeignKey(l => l.SecurityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
