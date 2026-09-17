namespace Portfolio.Application.MarketData;

public record DailyPricePoint(DateOnly Date, decimal Close, long? Volume);
