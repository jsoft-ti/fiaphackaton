using DonationService.Application.Common.Interfaces;
using MassTransit;

namespace DonationService.Infrastructure.Messaging;

/// <summary>
/// Thin adapter over MassTransit's <see cref="IPublishEndpoint"/>. When
/// resolved inside the same DI scope as the EF Core <see cref="Persistence.DonationDbContext"/>
/// change tracker (i.e. within a single HTTP request/handler execution),
/// MassTransit's EF Core Bus Outbox integration transparently intercepts
/// this call and defers actual delivery to RabbitMQ until the surrounding
/// <c>SaveChangesAsync</c> transaction commits.
/// </summary>
public sealed class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class =>
        _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
