using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;

namespace CampaignUserService.Api.Extensions;

/// <summary>
/// Converts Application-layer <see cref="Result"/>/<see cref="Result{T}"/>
/// outcomes into minimal API <see cref="IResult"/> responses, always using
/// RFC 7807 ProblemDetails for failures.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        var statusCode = MapStatusCode(result.Error.Type);

        return Results.Problem(
            statusCode: statusCode,
            title: MapTitle(result.Error.Type),
            detail: result.Error.Message,
            type: $"https://httpstatuses.io/{statusCode}",
            extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });
    }

    public static IResult ToOkOrProblem(this Result result) =>
        result.IsSuccess ? Results.Ok() : result.ToProblem();

    public static IResult ToOkOrProblem<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ((Result)result).ToProblem();

    public static IResult ToCreatedOrProblem<T>(this Result<T> result, string uri) =>
        result.IsSuccess ? Results.Created(uri, result.Value) : ((Result)result).ToProblem();

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };

    private static string MapTitle(ErrorType type) => type switch
    {
        ErrorType.Validation => "Erro de validação.",
        ErrorType.NotFound => "Recurso não encontrado.",
        ErrorType.Conflict => "Conflito de dados.",
        ErrorType.Unauthorized => "Não autorizado.",
        ErrorType.Forbidden => "Acesso negado.",
        _ => "Falha ao processar a requisição."
    };
}
