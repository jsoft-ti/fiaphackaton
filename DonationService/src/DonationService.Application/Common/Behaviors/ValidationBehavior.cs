using DonationService.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;

namespace DonationService.Application.Common.Behaviors;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> for the incoming
/// request before the handler executes (fail fast). Failures are surfaced
/// as a <see cref="ValidationAppException"/>, which the API's global
/// exception middleware turns into an RFC 7807 ProblemDetails 400 response -
/// keeping "malformed request" distinct from the <c>Result</c>-based
/// expected-business-failure path used for things like "campaign not found".
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToArray());

            throw new ValidationAppException(errors);
        }

        return await next();
    }
}
