using Microsoft.Extensions.DependencyInjection;

namespace Smpl.Mediatr;

/// <summary>
/// Typed dispatch: one wrapper instance per request type is cached, so no reflection invoke happens per call
/// and exceptions are never wrapped in TargetInvocationException.
/// </summary>
internal abstract class RequestHandlerWrapper<TResult>
{
    public abstract Task<TResult> HandleAsync(object request, IServiceProvider provider, CancellationToken ct);

    protected static Task<TResult> RunPipeline<TRequest>(
        TRequest request,
        IServiceProvider provider,
        RequestHandlerDelegate<TResult> handler,
        CancellationToken ct) where TRequest : notnull
    {
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResult>>().ToArray();
        var next = handler;
        // Last registered is innermost, first registered is outermost.
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var inner = next;
            next = token => behavior.HandleAsync(request, inner, token);
        }
        return next(ct);
    }
}

internal sealed class CommandWrapper<TCommand, TResult> : RequestHandlerWrapper<TResult>
    where TCommand : ICommand<TResult>
{
    public override Task<TResult> HandleAsync(object request, IServiceProvider provider, CancellationToken ct)
    {
        var command = (TCommand)request;
        var handler = provider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return RunPipeline(command, provider, token => handler.HandleAsync(command, token), ct);
    }
}

internal sealed class VoidCommandWrapper<TCommand> : RequestHandlerWrapper<Unit>
    where TCommand : ICommand
{
    public override Task<Unit> HandleAsync(object request, IServiceProvider provider, CancellationToken ct)
    {
        var command = (TCommand)request;
        var handler = provider.GetRequiredService<ICommandHandler<TCommand>>();
        return RunPipeline(command, provider, async token =>
        {
            await handler.HandleAsync(command, token).ConfigureAwait(false);
            return default(Unit);
        }, ct);
    }
}

internal sealed class QueryWrapper<TQuery, TResult> : RequestHandlerWrapper<TResult>
    where TQuery : IQuery<TResult>
{
    public override Task<TResult> HandleAsync(object request, IServiceProvider provider, CancellationToken ct)
    {
        var query = (TQuery)request;
        var handler = provider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return RunPipeline(query, provider, token => handler.HandleAsync(query, token), ct);
    }
}
