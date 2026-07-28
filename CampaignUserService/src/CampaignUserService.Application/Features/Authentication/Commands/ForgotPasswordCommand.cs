using System.Security.Cryptography;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Authentication.Commands;

/// <summary>
/// Starts the password recovery flow: generates a single-use token, persists
/// only its hash, and (in production) emails the raw token to the user.
/// Always returns success regardless of whether the email exists, to avoid
/// leaking which emails are registered (enumeration protection).
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ForgotPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IEmailSender emailSender,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ForgotPasswordCommand, Result>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken, includeRoles: false);

        if (user is null || !user.CanAuthenticate())
        {
            // Do not reveal account existence/state to the caller.
            return Result.Success();
        }

        await unitOfWork.PasswordResetTokens.InvalidateActiveTokensForUserAsync(user.Id, cancellationToken);

        var rawToken = GenerateSecureToken();
        var tokenHash = jwtTokenService.HashRefreshToken(rawToken);
        var utcNow = dateTimeProvider.UtcNow;

        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, utcNow.Add(TokenLifetime));
        unitOfWork.PasswordResetTokens.Add(resetToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailSender.SendPasswordResetEmailAsync(user.Email, user.FullName, rawToken, cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.PasswordResetRequested,
            "Recuperação de senha solicitada.",
            cancellationToken);

        return Result.Success();
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }
}
