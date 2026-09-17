using Portfolio.Application.Common;
using Portfolio.Application.Transactions.Dtos;

namespace Portfolio.Application.Transactions;

public interface ITransactionService
{
    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<PagedResult<TransactionResponse>?> GetTransactionsAsync(
        Guid userId, Guid portfolioId, int page, int pageSize, CancellationToken cancellationToken);

    /// <returns>Null if the portfolio does not exist or is not owned by <paramref name="userId"/>.</returns>
    Task<TransactionResponse?> CreateTransactionAsync(
        Guid userId, Guid portfolioId, CreateTransactionRequest request, CancellationToken cancellationToken);

    /// <returns>True if the transaction was found, owned by the user, and deleted.</returns>
    Task<bool> DeleteTransactionAsync(
        Guid userId, Guid portfolioId, Guid transactionId, CancellationToken cancellationToken);
}
