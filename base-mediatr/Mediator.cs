using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Smpl.Mediatr;

public class Mediator : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> CommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> VoidCommandWrappers = new();
    private static readonly ConcurrentDictionary<Type, object> QueryWrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var wrapper = (RequestHandlerWrapper<TResult>)CommandWrappers.GetOrAdd(command.GetType(),
            static (type, _) => Activator.CreateInstance(
                typeof(CommandWrapper<,>).MakeGenericType(type, typeof(TResult)))!, 0);
        return await wrapper.HandleAsync(command, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var wrapper = (RequestHandlerWrapper<Unit>)VoidCommandWrappers.GetOrAdd(command.GetType(),
            static (type, _) => Activator.CreateInstance(
                typeof(VoidCommandWrapper<>).MakeGenericType(type))!, 0);
        await wrapper.HandleAsync(command, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var wrapper = (RequestHandlerWrapper<TResult>)QueryWrappers.GetOrAdd(query.GetType(),
            static (type, _) => Activator.CreateInstance(
                typeof(QueryWrapper<,>).MakeGenericType(type, typeof(TResult)))!, 0);
        return await wrapper.HandleAsync(query, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        // Sequential and awaited; the first exception propagates.
        foreach (var handler in _serviceProvider.GetServices<INotificationHandler<TNotification>>())
        {
            await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
