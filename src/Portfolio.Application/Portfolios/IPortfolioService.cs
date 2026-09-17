using Portfolio.Application.Portfolios.Dtos;

namespace Portfolio.Application.Portfolios;

public interface IPortfolioService
{
    Task<IReadOnlyList<PortfolioResponse>> GetPortfoliosAsync(Guid userId, CancellationToken cancellationToken);

    Task<PortfolioResponse?> GetPortfolioAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);

    Task<PortfolioResponse> CreatePortfolioAsync(Guid userId, CreatePortfolioRequest request, CancellationToken cancellationToken);

    Task<PortfolioResponse?> UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioRequest request, CancellationToken cancellationToken);

    Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);
}
