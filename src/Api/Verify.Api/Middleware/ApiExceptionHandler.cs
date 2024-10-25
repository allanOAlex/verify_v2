using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Verify.Shared.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Verify.Api.Middleware;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> Logger)
    {
        logger = Logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exception occurred in the API.");

        var problemDetails = new ProblemDetails
        {
            Type = exception.GetType().Name,
            Instance = httpContext.Request.Path,
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "An error occurred while processing your request."
        };

        switch (exception)
        {
            case HttpRequestException:
                problemDetails.Status = (int)HttpStatusCode.ServiceUnavailable;
                problemDetails.Title = "Service Unavailable";
                //problemDetails.Detail = "Unable to connect to the server. Please try again later.";
                problemDetails.Detail = exception.Message;
                break;

            case NoContentException:
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "No Records Found";
                problemDetails.Detail = "The requested resource could not be found.";
                break;

            case CreatingDuplicateException:
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Duplicate Record";
                problemDetails.Detail = "The record already exists in the system.";
                break;

            case ValidationException validationException:
                problemDetails.Status = (int)HttpStatusCode.UnprocessableEntity;
                problemDetails.Title = "Validation Error";
                problemDetails.Detail = validationException.ValidationResult.ErrorMessage;

                var errors = new Dictionary<string, string[]>();
                foreach (var memberName in validationException.ValidationResult.MemberNames)
                {
                    errors.Add(memberName, new[] { validationException.ValidationResult.ErrorMessage! });
                }

                problemDetails.Extensions["errors"] = errors;
                break;

            case FluentValidation.ValidationException fluentValidationException:
                problemDetails.Status = (int)HttpStatusCode.UnprocessableEntity;
                problemDetails.Title = "Validation Error";
                problemDetails.Detail = "One or more validation errors occurred.";

                var errorList = new ModelStateDictionary();
                foreach (var error in fluentValidationException.Errors)
                {
                    errorList.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                problemDetails.Extensions["errors"] = errorList;
                break;

            default:
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

        return true;
    }


}
