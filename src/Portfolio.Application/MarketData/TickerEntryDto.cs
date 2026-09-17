namespace Portfolio.Application.MarketData;

public record TickerEntryDto(string Symbol, decimal LatestClose, decimal? ChangePercent);
