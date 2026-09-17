using Portfolio.Domain.Enums;

namespace Portfolio.Application.Transactions.Dtos;

public record TransactionResponse(
    Guid Id,
    string Symbol,
    TransactionType Type,
    decimal Quantity,
    decimal PricePerUnit,
    decimal Fees,
    DateOnly TradeDate,
    DateTimeOffset CreatedAt,
    decimal? RealizedGainLoss);
