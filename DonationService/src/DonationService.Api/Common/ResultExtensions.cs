using DonationService.SharedKernel.Common;
using DonationService.SharedKernel.Errors;

namespace DonationService.Api.Common;

/// <summary>Maps the Application layer's expected-failure <see cref="Result"/>/<see cref="Result{T}"/> to Minimal API responses.</summary>
public static class ResultExtensions
{
    public static IResult ToOkResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblemResult();

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value), result.Value)
            : result.Error.ToProblemResult();

    public static IResult ToNoContentResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblemResult();

    private static IResult ToProblemResult(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Dependency => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message,
            type: $"https://donationservice.errors/{error.Code}");
    }
}
