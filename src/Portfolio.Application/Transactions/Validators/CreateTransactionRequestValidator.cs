using FluentValidation;
using Portfolio.Application.Transactions.Dtos;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Transactions.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Fees).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TradeDate).NotEqual(default(DateOnly));
        RuleFor(x => x.TradeDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Trade date cannot be in the future.");
    }
}
