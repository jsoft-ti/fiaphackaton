using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CampaignUserService.Application.Common.Behaviors;

/// <summary>
/// Logs the start/end and elapsed time of every request handled through the
/// MediatR pipeline, and flags slow requests (performance logging).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "Slow request: {RequestName} took {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMilliseconds}ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Error handling {RequestName} after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
