using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Portfolios;
using Portfolio.Application.Portfolios.Dtos;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Portfolios;

public class PortfolioService(PortfolioDbContext db) : IPortfolioService
{
    public async Task<IReadOnlyList<PortfolioResponse>> GetPortfoliosAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.InvestmentPortfolios
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PortfolioResponse(p.Id, p.Name, p.BaseCurrency, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PortfolioResponse?> GetPortfolioAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        return await db.InvestmentPortfolios
            .Where(p => p.UserId == userId && p.Id == portfolioId)
            .Select(p => new PortfolioResponse(p.Id, p.Name, p.BaseCurrency, p.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PortfolioResponse> CreatePortfolioAsync(Guid userId, CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var entity = new Domain.Entities.InvestmentPortfolio
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            BaseCurrency = request.BaseCurrency,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.InvestmentPortfolios.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new PortfolioResponse(entity.Id, entity.Name, entity.BaseCurrency, entity.CreatedAt);
    }

    public async Task<PortfolioResponse?> UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.InvestmentPortfolios
            .SingleOrDefaultAsync(p => p.UserId == userId && p.Id == portfolioId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Name = request.Name;
        await db.SaveChangesAsync(cancellationToken);

        return new PortfolioResponse(entity.Id, entity.Name, entity.BaseCurrency, entity.CreatedAt);
    }

    public async Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        var entity = await db.InvestmentPortfolios
            .SingleOrDefaultAsync(p => p.UserId == userId && p.Id == portfolioId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        db.InvestmentPortfolios.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
