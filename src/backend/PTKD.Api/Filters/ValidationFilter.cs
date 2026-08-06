using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Collections.Generic;

namespace PTKD.API.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var arguments = context.ActionArguments.Values.ToList();
        
        foreach (var argument in arguments)
        {
            if (argument == null) continue;

            var type = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(type);
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;
            
            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(argument);
                var result = await validator.ValidateAsync(validationContext);
                if (!result.IsValid)
                {
                    throw new ValidationException(result.Errors);
                }
            }
        }

        await next();
    }
}
