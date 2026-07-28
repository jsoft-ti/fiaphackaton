using System.Diagnostics;
using DonationService.SharedKernel.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DonationService.Application.Common.Behaviors;

/// <summary>
/// Structured request/response logging for every MediatR command and query,
/// including elapsed processing time and the CorrelationId propagated from
/// the inbound HTTP request (or from the consumed integration event, on the
/// Worker side) - so a single donation can be traced end to end in Serilog.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = _currentUserService.CorrelationId;

        _logger.LogInformation(
            "Handling {RequestName} | CorrelationId={CorrelationId} | UserId={UserId}",
            requestName,
            correlationId,
            _currentUserService.UserId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms | CorrelationId={CorrelationId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "{RequestName} failed after {ElapsedMilliseconds}ms | CorrelationId={CorrelationId}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                correlationId);

            throw;
        }
    }
}
