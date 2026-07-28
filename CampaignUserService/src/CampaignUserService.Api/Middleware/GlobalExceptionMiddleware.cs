using System.Diagnostics;
using CampaignUserService.Domain.Exceptions;
using CampaignUserService.SharedKernel.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CampaignUserService.Api.Middleware;

/// <summary>
/// Catches every exception that escapes the pipeline and converts it into a
/// RFC 7807 ProblemDetails response. No raw exception or stack trace is ever
/// returned to the caller in production.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, code) = Map(exception);

        logger.LogError(
            exception,
            "Unhandled exception processing {Method} {Path}. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            Activity.Current?.Id ?? context.TraceIdentifier);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = context.Request.Path,
            Detail = exception.Message
        };

        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problemDetails.Extensions["code"] = code;

        if (exception is ValidationAppException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }
        else if (exception is ValidationException fluentValidationException)
        {
            problemDetails.Extensions["errors"] = fluentValidationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        if (environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static (int StatusCode, string Title, string Code) Map(Exception exception) => exception switch
    {
        ValidationAppException => (StatusCodes.Status400BadRequest, "Erro de validação.", "validation_error"),
        ValidationException => (StatusCodes.Status400BadRequest, "Erro de validação.", "validation_error"),
        NotFoundException notFound => (StatusCodes.Status404NotFound, "Recurso não encontrado.", notFound.Code),
        ConflictException conflict => (StatusCodes.Status409Conflict, "Conflito de dados.", conflict.Code),
        UnauthorizedAppException unauthorized => (StatusCodes.Status401Unauthorized, "Não autorizado.", unauthorized.Code),
        ForbiddenAppException forbidden => (StatusCodes.Status403Forbidden, "Acesso negado.", forbidden.Code),
        BusinessRuleException businessRule => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada.", businessRule.Code),
        DomainException => (StatusCodes.Status400BadRequest, "Requisição inválida.", "domain_error"),
        OperationCanceledException => (499, "Requisição cancelada.", "request_cancelled"),
        _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno inesperado.", "internal_server_error")
    };
}
