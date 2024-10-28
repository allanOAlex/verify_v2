using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Verify.Application.Extensions;
internal static class ApplicationExtensions
{
    public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }
}
