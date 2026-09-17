using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Identity;

namespace Portfolio.Infrastructure.Persistence;

public class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<InvestmentPortfolio> InvestmentPortfolios => Set<InvestmentPortfolio>();

    public DbSet<Security> Securities => Set<Security>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Lot> Lots => Set<Lot>();

    public DbSet<LotConsumption> LotConsumptions => Set<LotConsumption>();

    public DbSet<DailyPrice> DailyPrices => Set<DailyPrice>();

    public DbSet<PortfolioValueSnapshot> PortfolioValueSnapshots => Set<PortfolioValueSnapshot>();

    public DbSet<PriceRefreshLog> PriceRefreshLogs => Set<PriceRefreshLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Consistent precision for money/quantity fields across Postgres `numeric` columns.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 6);
    }
}
