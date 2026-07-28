using Serilog.Context;

namespace DonationService.Api.Middleware;

/// <summary>
/// Reads (or generates) the CorrelationId for every inbound request, stores
/// it on <c>HttpContext.Items</c> (where <c>ICurrentUserService.CorrelationId</c>
/// and <c>GlobalExceptionMiddleware</c> read it from), pushes it into the
/// Serilog LogContext so every log line for this request is tagged, and
/// echoes it back on the response so callers/consumers can correlate.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
