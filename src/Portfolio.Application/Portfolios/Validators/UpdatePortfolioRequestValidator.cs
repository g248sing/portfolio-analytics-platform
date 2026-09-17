using FluentValidation;
using Portfolio.Application.Portfolios.Dtos;

namespace Portfolio.Application.Portfolios.Validators;

public class UpdatePortfolioRequestValidator : AbstractValidator<UpdatePortfolioRequest>
{
    public UpdatePortfolioRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
