using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class LotConsumptionConfiguration : IEntityTypeConfiguration<LotConsumption>
{
    public void Configure(EntityTypeBuilder<LotConsumption> builder)
    {
        builder.HasKey(lc => lc.Id);

        // Computed, not persisted.
        builder.Ignore(lc => lc.RealizedGainLoss);
    }
}
