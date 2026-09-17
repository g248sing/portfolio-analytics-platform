namespace Portfolio.Application.Portfolios.Dtos;

public record CreatePortfolioRequest(string Name, string BaseCurrency = "USD");
