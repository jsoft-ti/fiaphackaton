using DonationService.Domain.Exceptions;
using DonationService.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DonationService.Api.Middleware;

/// <summary>
/// Catches every unhandled exception and converts it into an RFC 7807
/// ProblemDetails response - raw exceptions are never returned to callers.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var value) ? value?.ToString() : null;

        _logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path} | CorrelationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        var (statusCode, title, code) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://donationservice.errors/{code}",
            Instance = context.Request.Path,
            Detail = exception.Message,
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationAppException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static (int StatusCode, string Title, string Code) MapException(Exception exception) => exception switch
    {
        NotFoundException notFound => (StatusCodes.Status404NotFound, "Resource not found.", notFound.Code),
        ValidationAppException validation => (StatusCodes.Status400BadRequest, "One or more validation errors occurred.", validation.Code),
        DomainException domain => (StatusCodes.Status400BadRequest, "A business rule was violated.", "domain_rule_violation"),
        UnauthorizedAppException unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized.", unauthorized.Code),
        ForbiddenAppException forbidden => (StatusCodes.Status403Forbidden, "Forbidden.", forbidden.Code),
        UpstreamDependencyException dependency => (StatusCodes.Status502BadGateway, "An upstream dependency failed.", dependency.Code),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "internal_server_error"),
    };
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
