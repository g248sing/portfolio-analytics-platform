using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class InvestmentPortfolioConfiguration : IEntityTypeConfiguration<InvestmentPortfolio>
{
    public void Configure(EntityTypeBuilder<InvestmentPortfolio> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.BaseCurrency).HasMaxLength(3).IsRequired();

        builder.HasIndex(p => p.UserId);

        builder.HasMany(p => p.Transactions)
            .WithOne(t => t.InvestmentPortfolio)
            .HasForeignKey(t => t.InvestmentPortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Lots)
            .WithOne(l => l.InvestmentPortfolio)
            .HasForeignKey(l => l.InvestmentPortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ValueSnapshots)
            .WithOne(s => s.InvestmentPortfolio)
            .HasForeignKey(s => s.InvestmentPortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
