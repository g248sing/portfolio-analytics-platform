using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.HasKey(l => l.Id);

        // FIFO consumption order relies on this index.
        builder.HasIndex(l => new { l.SecurityId, l.InvestmentPortfolioId, l.AcquiredDate });

        builder.HasMany(l => l.Consumptions)
            .WithOne(lc => lc.Lot)
            .HasForeignKey(lc => lc.LotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
