using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => new { t.InvestmentPortfolioId, t.TradeDate });

        builder.HasOne(t => t.CreatedLot)
            .WithOne(l => l.BuyTransaction)
            .HasForeignKey<Lot>(l => l.BuyTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.LotConsumptions)
            .WithOne(lc => lc.SellTransaction)
            .HasForeignKey(lc => lc.SellTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
