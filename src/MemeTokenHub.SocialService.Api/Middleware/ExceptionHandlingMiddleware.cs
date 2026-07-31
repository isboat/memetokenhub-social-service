using Microsoft.AspNetCore.Mvc;

namespace MemeTokenHub.SocialService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        { await next(context); }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Request failed with correlation ID {CorrelationId}", context.TraceIdentifier);
            int status = exception switch { UnauthorizedAccessException => StatusCodes.Status401Unauthorized, KeyNotFoundException => StatusCodes.Status404NotFound, InvalidOperationException => StatusCodes.Status422UnprocessableEntity, _ => StatusCodes.Status500InternalServerError };
            ProblemDetails problem = new() { Status = status, Title = "The request could not be completed.", Detail = status == 500 ? "An unexpected error occurred." : exception.Message, Instance = context.Request.Path };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
        }
    }
}
