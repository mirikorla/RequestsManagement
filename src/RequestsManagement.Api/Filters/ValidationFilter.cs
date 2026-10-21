using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RequestsManagement.Api.Filters;

/// <summary>
/// Runs the registered FluentValidation validator for every action argument that has one,
/// so controllers never call validators by hand. Failures throw ValidationException,
/// which GlobalExceptionHandler turns into a 400 ValidationProblemDetails.
/// </summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid)
                throw new ValidationException(result.Errors);
        }

        await next();
    }
}
