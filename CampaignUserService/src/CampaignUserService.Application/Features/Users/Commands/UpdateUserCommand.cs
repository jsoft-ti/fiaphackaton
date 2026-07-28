using AutoMapper;
using CampaignUserService.Application.Common.Interfaces;
using CampaignUserService.Application.Features.Users.Dtos;
using CampaignUserService.Domain.Enums;
using CampaignUserService.Domain.Repositories;
using CampaignUserService.SharedKernel.Common;
using CampaignUserService.SharedKernel.Errors;
using CampaignUserService.SharedKernel.Interfaces;
using FluentValidation;
using MediatR;

namespace CampaignUserService.Application.Features.Users.Commands;

/// <summary>GestorOng-only update of any user's profile data (PUT /users/{id}).</summary>
public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? PhotoUrl,
    DateOnly? BirthDate) : IRequest<Result<UserDto>>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9\s\-\(\)]{8,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Telefone inválido.");
    }
}

public sealed class UpdateUserCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.Id, cancellationToken);

        if (user is null || user.IsDeleted)
        {
            return Result.Failure<UserDto>(Error.NotFound("user_not_found", "Usuário não encontrado."));
        }

        user.UpdateProfile(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.PhotoUrl,
            request.BirthDate,
            dateTimeProvider.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            user.Id,
            AuditActionType.UserUpdated,
            $"Usuário {user.Email} atualizado por um GestorOng.",
            cancellationToken);

        return Result.Success(mapper.Map<UserDto>(user));
    }
}
