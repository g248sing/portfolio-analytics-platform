namespace Portfolio.Application.Export;

public interface IExportService
{
    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<byte[]?> ExportHoldingsToCsvAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);

    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<byte[]?> ExportHoldingsToXlsxAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken);
}
