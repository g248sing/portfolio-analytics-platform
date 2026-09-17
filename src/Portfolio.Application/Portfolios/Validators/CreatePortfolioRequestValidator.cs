using FluentValidation;
using Portfolio.Application.Portfolios.Dtos;

namespace Portfolio.Application.Portfolios.Validators;

public class CreatePortfolioRequestValidator : AbstractValidator<CreatePortfolioRequest>
{
    public CreatePortfolioRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BaseCurrency).NotEmpty().Length(3);
    }
}
