using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProductCatalog.Api.Validation;

/// <summary>
/// Ejecuta el validador de FluentValidation registrado para cada argumento de la acción y
/// devuelve un 400 <see cref="ValidationProblemDetails"/> con todos los errores, con el mismo
/// formato que la validación de modelo integrada de <c>[ApiController]</c>.
/// </summary>
internal sealed class FluentValidationFilter : IAsyncActionFilter
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

            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
            {
                context.ModelState.AddModelError(JsonNamingPolicy.CamelCase.ConvertName(error.PropertyName), error.ErrorMessage);
            }
        }

        if (!context.ModelState.IsValid)
        {
            var controller = (ControllerBase)context.Controller;
            context.Result = controller.ValidationProblem(context.ModelState);
            return;
        }

        await next();
    }
}
