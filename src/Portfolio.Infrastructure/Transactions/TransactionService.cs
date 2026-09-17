using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common;
using Portfolio.Application.Transactions;
using Portfolio.Application.Transactions.Dtos;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Exceptions;
using Portfolio.Domain.Services;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Transactions;

public class TransactionService(PortfolioDbContext db) : ITransactionService
{
    public async Task<PagedResult<TransactionResponse>?> GetTransactionsAsync(
        Guid userId, Guid portfolioId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await OwnsPortfolioAsync(userId, portfolioId, cancellationToken))
        {
            return null;
        }

        var query = db.Transactions
            .Where(t => t.InvestmentPortfolioId == portfolioId)
            .OrderByDescending(t => t.TradeDate)
            .ThenByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionResponse(
                t.Id,
                t.Security.Symbol,
                t.Type,
                t.Quantity,
                t.PricePerUnit,
                t.Fees,
                t.TradeDate,
                t.CreatedAt,
                t.Type == TransactionType.Sell
                    ? t.LotConsumptions.Sum(lc => (decimal?)(lc.ProceedsAllocated - lc.CostBasisConsumed))
                    : null))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionResponse>(items, totalCount, page, pageSize);
    }

    public async Task<TransactionResponse?> CreateTransactionAsync(
        Guid userId, Guid portfolioId, CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsPortfolioAsync(userId, portfolioId, cancellationToken))
        {
            return null;
        }

        return request.Type switch
        {
            TransactionType.Buy => await CreateBuyAsync(portfolioId, request, cancellationToken),
            TransactionType.Sell => await CreateSellAsync(portfolioId, request, cancellationToken),
            TransactionType.Dividend => await CreateDividendAsync(portfolioId, request, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(request), "Unknown transaction type."),
        };
    }

    public async Task<bool> DeleteTransactionAsync(
        Guid userId, Guid portfolioId, Guid transactionId, CancellationToken cancellationToken)
    {
        if (!await OwnsPortfolioAsync(userId, portfolioId, cancellationToken))
        {
            return false;
        }

        var transaction = await db.Transactions
            .Include(t => t.CreatedLot)
            .Include(t => t.LotConsumptions)
            .ThenInclude(lc => lc.Lot)
            .SingleOrDefaultAsync(t => t.Id == transactionId && t.InvestmentPortfolioId == portfolioId, cancellationToken);

        if (transaction is null)
        {
            return false;
        }

        switch (transaction.Type)
        {
            case TransactionType.Buy when transaction.CreatedLot is not null:
                if (transaction.CreatedLot.RemainingQuantity != transaction.CreatedLot.OriginalQuantity)
                {
                    throw new LotAlreadyConsumedException();
                }

                db.Lots.Remove(transaction.CreatedLot);
                break;

            case TransactionType.Sell:
                foreach (var consumption in transaction.LotConsumptions)
                {
                    consumption.Lot.RemainingQuantity += consumption.QuantityConsumed;
                }

                db.LotConsumptions.RemoveRange(transaction.LotConsumptions);
                break;
        }

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<TransactionResponse> CreateBuyAsync(Guid portfolioId, CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var security = await GetOrCreateSecurityAsync(request.Symbol, cancellationToken);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InvestmentPortfolioId = portfolioId,
            Security = security,
            Type = TransactionType.Buy,
            Quantity = request.Quantity,
            PricePerUnit = request.PricePerUnit,
            Fees = request.Fees,
            TradeDate = request.TradeDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var costBasisPerUnit = (request.Quantity * request.PricePerUnit + request.Fees) / request.Quantity;

        var lot = new Lot
        {
            Id = Guid.NewGuid(),
            InvestmentPortfolioId = portfolioId,
            Security = security,
            BuyTransaction = transaction,
            OriginalQuantity = request.Quantity,
            RemainingQuantity = request.Quantity,
            CostBasisPerUnit = costBasisPerUnit,
            AcquiredDate = request.TradeDate,
        };

        db.Transactions.Add(transaction);
        db.Lots.Add(lot);
        await db.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id, security.Symbol, transaction.Type, transaction.Quantity,
            transaction.PricePerUnit, transaction.Fees, transaction.TradeDate, transaction.CreatedAt, null);
    }

    private async Task<TransactionResponse> CreateSellAsync(Guid portfolioId, CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var normalizedSymbol = request.Symbol.Trim().ToUpperInvariant();
        var security = await db.Securities.SingleOrDefaultAsync(s => s.Symbol == normalizedSymbol, cancellationToken);

        var openLots = security is null
            ? []
            : await db.Lots
                .Include(l => l.BuyTransaction)
                .Where(l => l.InvestmentPortfolioId == portfolioId && l.SecurityId == security.Id && l.RemainingQuantity > 0)
                .OrderBy(l => l.AcquiredDate)
                .ThenBy(l => l.BuyTransaction.CreatedAt)
                .ToListAsync(cancellationToken);

        security ??= new Security { Id = Guid.NewGuid(), Symbol = normalizedSymbol, Name = normalizedSymbol };

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InvestmentPortfolioId = portfolioId,
            Security = security,
            Type = TransactionType.Sell,
            Quantity = request.Quantity,
            PricePerUnit = request.PricePerUnit,
            Fees = request.Fees,
            TradeDate = request.TradeDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var totalNetProceeds = request.Quantity * request.PricePerUnit - request.Fees;
        var consumptions = FifoLotMatcher.Consume(openLots, transaction.Id, request.Quantity, totalNetProceeds);

        db.Transactions.Add(transaction);
        db.LotConsumptions.AddRange(consumptions);
        await db.SaveChangesAsync(cancellationToken);

        var realizedGainLoss = consumptions.Sum(c => c.ProceedsAllocated - c.CostBasisConsumed);

        return new TransactionResponse(
            transaction.Id, security.Symbol, transaction.Type, transaction.Quantity,
            transaction.PricePerUnit, transaction.Fees, transaction.TradeDate, transaction.CreatedAt, realizedGainLoss);
    }

    private async Task<TransactionResponse> CreateDividendAsync(Guid portfolioId, CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var security = await GetOrCreateSecurityAsync(request.Symbol, cancellationToken);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            InvestmentPortfolioId = portfolioId,
            Security = security,
            Type = TransactionType.Dividend,
            Quantity = request.Quantity,
            PricePerUnit = request.PricePerUnit,
            Fees = request.Fees,
            TradeDate = request.TradeDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken);

        return new TransactionResponse(
            transaction.Id, security.Symbol, transaction.Type, transaction.Quantity,
            transaction.PricePerUnit, transaction.Fees, transaction.TradeDate, transaction.CreatedAt, null);
    }

    private async Task<Security> GetOrCreateSecurityAsync(string symbol, CancellationToken cancellationToken)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        var security = await db.Securities.SingleOrDefaultAsync(s => s.Symbol == normalized, cancellationToken);
        if (security is not null)
        {
            return security;
        }

        security = new Security { Id = Guid.NewGuid(), Symbol = normalized, Name = normalized };
        db.Securities.Add(security);
        return security;
    }

    private async Task<bool> OwnsPortfolioAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        return await db.InvestmentPortfolios.AnyAsync(p => p.Id == portfolioId && p.UserId == userId, cancellationToken);
    }
}
