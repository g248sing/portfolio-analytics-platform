namespace Portfolio.Application.Analytics;

public interface IAnalyticsService
{
    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<IReadOnlyList<ValueHistoryPoint>?> GetValueHistoryAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);

    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<AllocationResponse?> GetAllocationAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);

    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<PerformersResponse?> GetPerformersAsync(Guid userId, Guid portfolioId, int count, CancellationToken cancellationToken);
}
