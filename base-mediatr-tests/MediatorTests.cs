using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using base_mediatr;
using Xunit;

public class MediatorTests
{
    public class TestCommand : ICommand { }
    public class TestCommandWithResponse : ICommand<string> { }
    public class TestQuery : IQuery<int> { }

    public class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class TestCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, string>
    {
        public Task<string> HandleAsync(TestCommandWithResponse command, CancellationToken cancellationToken = default)
        {
            return Task.FromResult("response");
        }
    }

    public class TestQueryHandler : IQueryHandler<TestQuery, int>
    {
        public Task<int> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(42);
        }
    }
    
    

    [Fact]
    public async Task SendAsync_WithCommand_CallsCorrectHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommand>, TestCommandHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new TestCommand());
    }

    [Fact]
    public async Task SendAsync_WithCommandWithResponse_ReturnsExpectedResult()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<TestCommandWithResponse, string>, TestCommandWithResponseHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new TestCommandWithResponse());

        Assert.Equal("response", result);
    }

    [Fact]
    public async Task SendAsync_WithQuery_ReturnsExpectedResult()
    {
        var services = new ServiceCollection();
        services.AddScoped<IQueryHandler<TestQuery, int>, TestQueryHandler>();
        services.AddSingleton<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new TestQuery());

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task SendAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.SendAsync((TestCommand)null));
    }

    [Fact]
    public async Task SendAsync_WithUnregisteredHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator, Mediator>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(new TestCommand()));
    }

}
