using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Portfolio.Api.Filters;

/// <summary>
/// Runs any registered FluentValidation <see cref="IValidator{T}"/> against action
/// arguments before the action executes, returning a 400 ValidationProblem on failure.
/// </summary>
public class FluentValidationActionFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                foreach (var error in result.Errors)
                {
                    context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                context.Result = new ValidationProblemDetails(context.ModelState).ToActionResult();
                return;
            }
        }

        await next();
    }
}

file static class ValidationProblemDetailsExtensions
{
    public static ObjectResult ToActionResult(this ValidationProblemDetails problemDetails)
    {
        problemDetails.Status = StatusCodes.Status400BadRequest;
        return new BadRequestObjectResult(problemDetails);
    }
}
