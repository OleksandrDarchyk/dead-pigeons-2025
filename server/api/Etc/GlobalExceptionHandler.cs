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
            Status = statusCode,
            Title = exception.Message
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}