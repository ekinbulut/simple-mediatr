using System;
using base_mediatr;
using Xunit;

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

