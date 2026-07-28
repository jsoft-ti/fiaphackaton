namespace DonationService.Application.Common.Interfaces;

/// <summary>
/// Application-owned abstraction over the message bus. Implemented in
/// Infrastructure by wrapping MassTransit's <c>IPublishEndpoint</c>, so a
/// call to <see cref="PublishAsync{TEvent}"/> made inside the same DI scope
/// as the EF Core <c>DbContext</c> automatically participates in the
/// transactional (EF Core) Bus Outbox: the message is persisted alongside
/// the aggregate in the same database transaction and only handed to
/// RabbitMQ after that transaction commits successfully.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
