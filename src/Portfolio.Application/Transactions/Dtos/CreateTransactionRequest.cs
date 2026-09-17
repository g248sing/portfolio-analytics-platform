using Portfolio.Domain.Enums;

namespace Portfolio.Application.Transactions.Dtos;

public record CreateTransactionRequest(
    string Symbol,
    TransactionType Type,
    decimal Quantity,
    decimal PricePerUnit,
    decimal Fees,
    DateOnly TradeDate);
