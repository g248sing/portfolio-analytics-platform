using Portfolio.Domain.Enums;

namespace Portfolio.Application.Holdings;

public record HoldingResponse(
    string Symbol,
    string SecurityName,
    string? Sector,
    AssetClass AssetClass,
    decimal Quantity,
    decimal AverageCostBasisPerUnit,
    decimal TotalCostBasis,
    decimal? CurrentPrice,
    decimal? MarketValue,
    decimal? UnrealizedGainLoss,
    decimal? UnrealizedGainLossPercent,
    decimal RealizedGainLoss);
