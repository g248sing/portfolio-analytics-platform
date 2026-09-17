namespace Portfolio.Application.Holdings;

public interface IHoldingsService
{
    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<IReadOnlyList<HoldingResponse>?> GetHoldingsAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);
}
