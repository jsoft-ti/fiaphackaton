using AutoMapper;
using DonationService.Application.Features.Donations.Mappings;
using DonationService.Application.Features.Donations.Queries.GetDonationById;
using DonationService.Domain.ReadModels;
using DonationService.Domain.Repositories;
using DonationService.SharedKernel.Errors;
using DonationService.SharedKernel.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace DonationService.UnitTests.Application;

public class GetDonationByIdQueryHandlerTests
{
    private readonly Mock<IDonationReadRepository> _readRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly IMapper _mapper;

    public GetDonationByIdQueryHandlerTests()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<DonationMappingProfile>());
        _mapper = configuration.CreateMapper();
    }

    private GetDonationByIdQueryHandler CreateHandler() =>
        new(_readRepository.Object, _currentUserService.Object, _mapper);

    private static DonationReadModel CreateReadModel(Guid donationId, Guid userId) => new(
        donationId, Guid.NewGuid(), userId, "Jane Doe", "jane@example.com", 100m,
        "BRL", "Pix", DateTime.UtcNow, "Confirmed", DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Handle_WhenDonationNotFound_ShouldReturnNotFound()
    {
        var donationId = Guid.NewGuid();

        _readRepository
            .Setup(r => r.GetByIdAsync(donationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DonationReadModel?)null);

        var result = await CreateHandler().Handle(new GetDonationByIdQuery(donationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsOwner_ShouldReturnDonation()
    {
        var donationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _readRepository
            .Setup(r => r.GetByIdAsync(donationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReadModel(donationId, userId));

        _currentUserService.SetupGet(c => c.UserId).Returns(userId);
        _currentUserService.SetupGet(c => c.Role).Returns("Doador");

        var result = await CreateHandler().Handle(new GetDonationByIdQuery(donationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(donationId);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNeitherOwnerNorGestor_ShouldReturnForbidden()
    {
        var donationId = Guid.NewGuid();

        _readRepository
            .Setup(r => r.GetByIdAsync(donationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReadModel(donationId, Guid.NewGuid()));

        _currentUserService.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        _currentUserService.SetupGet(c => c.Role).Returns("Doador");

        var result = await CreateHandler().Handle(new GetDonationByIdQuery(donationId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenCallerIsGestorOng_ShouldReturnDonationRegardlessOfOwnership()
    {
        var donationId = Guid.NewGuid();

        _readRepository
            .Setup(r => r.GetByIdAsync(donationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReadModel(donationId, Guid.NewGuid()));

        _currentUserService.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        _currentUserService.SetupGet(c => c.Role).Returns("GestorOng");

        var result = await CreateHandler().Handle(new GetDonationByIdQuery(donationId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
