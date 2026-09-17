using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Extensions;
using Portfolio.Application.Transactions;
using Portfolio.Application.Transactions.Dtos;
using Portfolio.Domain.Exceptions;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolios/{portfolioId:guid}/transactions")]
public class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid portfolioId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await transactionService.GetTransactionsAsync(User.GetUserId(), portfolioId, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid portfolioId, CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await transactionService.CreateTransactionAsync(User.GetUserId(), portfolioId, request, cancellationToken);
            return created is null ? NotFound() : Ok(created);
        }
        catch (InsufficientSharesException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message });
        }
    }

    [HttpDelete("{transactionId:guid}")]
    public async Task<IActionResult> Delete(Guid portfolioId, Guid transactionId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await transactionService.DeleteTransactionAsync(User.GetUserId(), portfolioId, transactionId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (LotAlreadyConsumedException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message });
        }
    }
}
