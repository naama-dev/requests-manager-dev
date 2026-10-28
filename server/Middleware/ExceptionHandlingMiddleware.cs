using Microsoft.AspNetCore.Mvc;

namespace RequestsManager.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unhandled exception occurred while processing the request.");

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();

            var (statusCode, title) = ex switch
            {
                KeyNotFoundException =>
                    (StatusCodes.Status404NotFound, "Resource not found"),

                ArgumentException =>
                    (StatusCodes.Status400BadRequest, "Invalid request"),

                InvalidOperationException =>
                    (StatusCodes.Status409Conflict, "Conflict"),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "An unexpected error occurred")
            };

            context.Response.StatusCode = statusCode;

            await Results.Problem(
                statusCode: statusCode,
                title: title,
                instance: context.Request.Path
            ).ExecuteAsync(context);
        }
    }
}