using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Persistence.Configurations;

public class DailyPriceConfiguration : IEntityTypeConfiguration<DailyPrice>
{
    public void Configure(EntityTypeBuilder<DailyPrice> builder)
    {
        builder.HasKey(dp => dp.Id);

        builder.HasIndex(dp => new { dp.SecurityId, dp.Date }).IsUnique();
    }
}
