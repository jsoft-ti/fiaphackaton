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

/// <summary>Self-service profile update (PUT /users/me). Available to both roles.</summary>
public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? PhotoUrl,
    DateOnly? BirthDate) : IRequest<Result<UserDto>>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9\s\-\(\)]{8,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Telefone inválido.");
        RuleFor(x => x.BirthDate)
            .LessThan(x => DateOnly.FromDateTime(DateTime.UtcNow))
            .When(x => x.BirthDate.HasValue)
            .WithMessage("Data de nascimento inválida.");
        RuleFor(x => x.PhotoUrl).MaximumLength(2048);
    }
}

public sealed class UpdateProfileCommandHandler(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IAuditService auditService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateProfileCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);

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
            "Usuário atualizou o próprio perfil.",
            cancellationToken);

        return Result.Success(mapper.Map<UserDto>(user));
    }
}
