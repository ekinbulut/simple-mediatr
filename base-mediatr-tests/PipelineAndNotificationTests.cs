using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Smpl.Mediatr;
using Smpl.Mediatr.Extension;

namespace PipelineTests;

public class Trace
{
    public List<string> Log { get; } = new();
}

public record Ping(string Value) : IQuery<string>;
public record Add(int A, int B) : ICommand<int>;
public record DoIt : ICommand;
public record Boom : ICommand<int>;
public record Happened : INotification;
public record Exploded : INotification;

public class PingHandler : IQueryHandler<Ping, string>
{
    private readonly Trace _trace;
    public PingHandler(Trace trace) => _trace = trace;
    public Task<string> HandleAsync(Ping query, CancellationToken cancellationToken = default)
    {
        _trace.Log.Add("handler");
        return Task.FromResult(query.Value + "!");
    }
}

public class AddHandler : ICommandHandler<Add, int>
{
    public Task<int> HandleAsync(Add command, CancellationToken cancellationToken = default)
        => Task.FromResult(command.A + command.B);
}

public class DoItHandler : ICommandHandler<DoIt>
{
    private readonly Trace _trace;
    public DoItHandler(Trace trace) => _trace = trace;
    public Task HandleAsync(DoIt command, CancellationToken cancellationToken = default)
    {
        _trace.Log.Add("handler");
        return Task.CompletedTask;
    }
}

public class BoomHandler : ICommandHandler<Boom, int>
{
    public Task<int> HandleAsync(Boom command, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("boom");
}

public class FirstHandler : INotificationHandler<Happened>
{
    private readonly Trace _trace;
    public FirstHandler(Trace trace) => _trace = trace;
    public Task HandleAsync(Happened notification, CancellationToken cancellationToken = default)
    {
        _trace.Log.Add("first");
        return Task.CompletedTask;
    }
}

public class SecondHandler : INotificationHandler<Happened>
{
    private readonly Trace _trace;
    public SecondHandler(Trace trace) => _trace = trace;
    public Task HandleAsync(Happened notification, CancellationToken cancellationToken = default)
    {
        _trace.Log.Add("second");
        return Task.CompletedTask;
    }
}

public class OuterBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult> where TRequest : notnull
{
    private readonly Trace _trace;
    public OuterBehavior(Trace trace) => _trace = trace;
    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        _trace.Log.Add("outer-in");
        var r = await next(ct);
        _trace.Log.Add("outer-out");
        return r;
    }
}

public class InnerBehavior<TRequest, TResult> : IPipelineBehavior<TRequest, TResult> where TRequest : notnull
{
    private readonly Trace _trace;
    public InnerBehavior(Trace trace) => _trace = trace;
    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken ct)
    {
        _trace.Log.Add("inner-in");
        var r = await next(ct);
        _trace.Log.Add("inner-out");
        return r;
    }
}

public class PipelineAndNotificationTests
{
    private static (IMediator Mediator, Trace Trace, ServiceProvider Provider) Build(bool behaviors = true)
    {
        var services = new ServiceCollection();
        var trace = new Trace();
        services.AddSingleton(trace);
        services.AddMediator(Assembly.GetExecutingAssembly());
        if (behaviors)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(OuterBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(InnerBehavior<,>));
        }
        var provider = services.BuildServiceProvider();
        return (provider.CreateScope().ServiceProvider.GetRequiredService<IMediator>(), trace, provider);
    }

    [Fact]
    public async Task Behaviors_RunFirstRegisteredOutermost_ForQuery()
    {
        var (mediator, trace, _) = Build();

        var result = await mediator.SendAsync(new Ping("x"));

        Assert.Equal("x!", result);
        Assert.Equal(new[] { "outer-in", "inner-in", "handler", "inner-out", "outer-out" }, trace.Log);
    }

    [Fact]
    public async Task Behaviors_RunForCommandWithResult()
    {
        var (mediator, trace, _) = Build();

        var result = await mediator.SendAsync(new Add(2, 3));

        Assert.Equal(5, result);
        Assert.Equal(new[] { "outer-in", "inner-in", "inner-out", "outer-out" }, trace.Log);
    }

    [Fact]
    public async Task VoidCommand_FlowsThroughPipelineWithUnit()
    {
        var (mediator, trace, _) = Build();

        await mediator.SendAsync(new DoIt());

        Assert.Equal(new[] { "outer-in", "inner-in", "handler", "inner-out", "outer-out" }, trace.Log);
    }

    [Fact]
    public async Task VoidCommand_BehaviorSeesUnitResult()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Trace());
        services.AddMediator(Assembly.GetExecutingAssembly());
        services.AddScoped<IPipelineBehavior<DoIt, Unit>, UnitCaptureBehavior>();
        var mediator = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new DoIt());

        Assert.True(UnitCaptureBehavior.Seen);
    }

    private class UnitCaptureBehavior : IPipelineBehavior<DoIt, Unit>
    {
        public static bool Seen;
        public async Task<Unit> HandleAsync(DoIt request, RequestHandlerDelegate<Unit> next, CancellationToken ct)
        {
            var u = await next(ct);
            Seen = u.Equals(default(Unit));
            return u;
        }
    }

    [Fact]
    public async Task NoBehaviors_StillDispatches()
    {
        var (mediator, trace, _) = Build(behaviors: false);

        Assert.Equal(7, await mediator.SendAsync(new Add(3, 4)));
        Assert.Empty(trace.Log);
    }

    [Fact]
    public async Task Notification_GoesToAllHandlers()
    {
        var (mediator, trace, _) = Build(behaviors: false);

        await mediator.PublishAsync(new Happened());

        Assert.Equal(2, trace.Log.Count);
        Assert.Contains("first", trace.Log);
        Assert.Contains("second", trace.Log);
    }

    [Fact]
    public void Mediator_IsAlsoResolvableAsPublisher()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        Assert.True(typeof(IPublisher).IsAssignableFrom(typeof(IMediator)));
        Assert.NotNull(services.BuildServiceProvider().CreateScope().ServiceProvider.GetService<IPublisher>());
    }

    [Fact]
    public async Task Notification_FirstExceptionPropagates_AndStopsLaterHandlers()
    {
        var services = new ServiceCollection();
        var trace = new Trace();
        services.AddSingleton(trace);
        services.AddScoped<INotificationHandler<Exploded>, ThrowingHandler>();
        services.AddScoped<INotificationHandler<Exploded>, AfterThrowHandler>();
        services.AddScoped<IMediator, Mediator>();
        var mediator = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.PublishAsync(new Exploded()));

        Assert.Equal("notify-boom", ex.Message);
        Assert.Empty(trace.Log);
    }

    private class AfterThrowHandler : INotificationHandler<Exploded>
    {
        private readonly Trace _trace;
        public AfterThrowHandler(Trace trace) => _trace = trace;
        public Task HandleAsync(Exploded notification, CancellationToken cancellationToken = default)
        {
            _trace.Log.Add("after");
            return Task.CompletedTask;
        }
    }

    private class ThrowingHandler : INotificationHandler<Exploded>
    {
        public Task HandleAsync(Exploded notification, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("notify-boom");
    }

    [Fact]
    public async Task HandlerException_PropagatesUnwrapped()
    {
        var (mediator, _, _) = Build();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new Boom()));

        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task HandlerException_PropagatesUnwrapped_WithoutBehaviors()
    {
        var (mediator, _, _) = Build(behaviors: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new Boom()));
    }

    [Fact]
    public void ParameterlessAddMediator_IsObsolete()
    {
        var method = typeof(ServiceCollectionExtensions).GetMethod(
            nameof(ServiceCollectionExtensions.AddMediator), new[] { typeof(IServiceCollection) });

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<ObsoleteAttribute>());
    }
}
