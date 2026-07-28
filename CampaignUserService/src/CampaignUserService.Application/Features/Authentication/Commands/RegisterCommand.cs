using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Authentication.Dtos;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Authentication.Commands;

/// <summary>
/// Public self-registration. Always creates the account with the "Doador"
/// role - GestorOng accounts can only be created by an existing GestorOng
/// through <see cref="Users.Commands.CreateUserCommand"/>.
/// </summary>
public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber,
    string? Cpf,
    DateOnly? BirthDate,
    string IpAddress,
    string? UserAgent) : IRequest<Result<AuthResultDto>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter ao menos um caractere especial.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("As senhas não coincidem.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9\s\-\(\)]{8,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Telefone inválido.");

        RuleFor(x => x.Cpf)
            .Matches(@"^\d{11}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF inválido. Informe apenas os 11 dígitos.");

        RuleFor(x => x.BirthDate)
            .LessThan(x => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.BirthDate.HasValue)
            .WithMessage("Data de nascimento inválida.");
    }
}

public sealed class RegisterCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RegisterCommand, Result<AuthResultDto>>
{
    public async Task<Result<AuthResultDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Users.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<AuthResultDto>(
                Error.Conflict("email_already_used", "Já existe uma conta cadastrada com este email."));
        }

        if (!string.IsNullOrWhiteSpace(request.Cpf) &&
            await unitOfWork.Users.ExistsByCpfAsync(request.Cpf, cancellationToken))
        {
            return Result.Failure<AuthResultDto>(
                Error.Conflict("cpf_already_used", "Já existe uma conta cadastrada com este CPF."));
        }

        var doadorRole = await unitOfWork.Roles.GetByNameAsync(RoleName.Doador, cancellationToken)
            ?? throw new InvalidOperationException("A role 'Doador' não está cadastrada. Verifique o seed do banco de dados.");

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            passwordHash,
            request.PhoneNumber,
            request.Cpf,
            request.BirthDate);

        user.AssignRole(doadorRole, dateTimeProvider.UtcNow);

        unitOfWork.Users.Add(user);

        var accessToken = jwtTokenService.GenerateAccessToken(user, doadorRole.Name.ToString());
        var rawRefreshToken = jwtTokenService.GenerateRefreshTokenValue();
        var refreshTokenHash = jwtTokenService.HashRefreshToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            dateTimeProvider.UtcNow.Add(jwtTokenService.RefreshTokenLifetime),
            request.IpAddress,
            request.UserAgent);

        unitOfWork.RefreshTokens.Add(refreshToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.UserRegistered,
            $"Usuário {user.Email} registrado com sucesso.",
            cancellationToken);

        await emailSender.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);

        return Result.Success(new AuthResultDto(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresAtUtc,
            "Bearer",
            user.Id,
            user.Email,
            user.FullName,
            doadorRole.Name.ToString()));
    }
}
