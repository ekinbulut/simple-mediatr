namespace Smpl.Mediatr;

public interface INotification
{
}

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}

public interface IPublisher
{
    /// <summary>
    /// Publishes to all handlers sequentially, in registration order. The first exception propagates (fail-fast).
    /// </summary>
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
