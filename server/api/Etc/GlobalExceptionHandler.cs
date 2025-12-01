using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BogusValidationException = Bogus.ValidationException;
using DataAnnotationValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace api.Etc;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            BogusValidationException => StatusCodes.Status400BadRequest,
            DataAnnotationValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode
        };

        switch (exception)
        {
            case DataAnnotationValidationException dataEx:
               
                problemDetails.Title = "Validation error";
                problemDetails.Detail =
                    dataEx.ValidationResult?.ErrorMessage
                    ?? dataEx.Message;
                break;

            case BogusValidationException bogusEx:
                problemDetails.Title = "Validation error";
                problemDetails.Detail = bogusEx.Message;
                break;

            case UnauthorizedAccessException:
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = "You are not allowed to perform this action.";
                break;

            default:
                
                problemDetails.Title = "Server error";
                problemDetails.Detail = "An unexpected server error occurred.";
                break;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
