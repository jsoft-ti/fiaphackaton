using AutoMapper;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Users.Dtos;
using CampaignUserService.Domain.Entities;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>
/// GestorOng-only user creation (POST /users). Used to onboard both new
/// Doadores and new Gestores ("Cadastrar outros gestores").
/// </summary>
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber,
    string? Cpf,
    DateOnly? BirthDate,
    RoleName Role) : IRequest<Result<UserDto>>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("A senha deve conter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve conter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve conter ao menos um número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve conter ao menos um caractere especial.");

        RuleFor(x => x.Cpf)
            .Matches(@"^\d{11}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf))
            .WithMessage("CPF inválido. Informe apenas os 11 dígitos.");

        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class CreateUserCommandHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IMapper mapper,
    IAuditService auditService,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.Users.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<UserDto>(
                Error.Conflict("email_already_used", "Já existe uma conta cadastrada com este email."));
        }

        if (!string.IsNullOrWhiteSpace(request.Cpf) &&
            await unitOfWork.Users.ExistsByCpfAsync(request.Cpf, cancellationToken))
        {
            return Result.Failure<UserDto>(
                Error.Conflict("cpf_already_used", "Já existe uma conta cadastrada com este CPF."));
        }

        var role = await unitOfWork.Roles.GetByNameAsync(request.Role, cancellationToken)
            ?? throw new InvalidOperationException($"A role '{request.Role}' não está cadastrada.");

        var user = User.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            passwordHasher.Hash(request.Password),
            request.PhoneNumber,
            request.Cpf,
            request.BirthDate);

        user.AssignRole(role, dateTimeProvider.UtcNow);

        unitOfWork.Users.Add(user);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.UserCreated,
            $"Usuário {user.Email} criado por um GestorOng com a role {role.Name}.",
            cancellationToken);

        await emailSender.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);

        return Result.Success(mapper.Map<UserDto>(user));
    }
}
