namespace Portfolio.Application.Portfolios.Dtos;

public record PortfolioResponse(Guid Id, string Name, string BaseCurrency, DateTimeOffset CreatedAt);
