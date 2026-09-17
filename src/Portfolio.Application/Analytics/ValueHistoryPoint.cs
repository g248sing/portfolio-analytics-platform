namespace Portfolio.Application.Analytics;

public record ValueHistoryPoint(DateOnly Date, decimal TotalMarketValue, decimal TotalCostBasis);
