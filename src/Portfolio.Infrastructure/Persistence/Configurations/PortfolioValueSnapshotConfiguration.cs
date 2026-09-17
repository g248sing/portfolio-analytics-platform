using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class PortfolioValueSnapshotConfiguration : IEntityTypeConfiguration<PortfolioValueSnapshot>
{
    public void Configure(EntityTypeBuilder<PortfolioValueSnapshot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.InvestmentPortfolioId, s.Date }).IsUnique();
    }
}
