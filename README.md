# Base MediatR

A lightweight mediator pattern implementation for .NET 9, providing a simple and clean abstraction for handling commands and queries in your applications using CQRS (Command Query Responsibility Segregation) principles.

## Overview

This project implements the mediator pattern to decouple request/response handling in your application. It provides a clean separation between commands (operations that change state) and queries (operations that retrieve data), promoting clean architecture and separation of concerns.

## Features

- **Command Pattern**: Execute operations with or without return values
- **Query Pattern**: Retrieve data without side effects
- **Mediator Pattern**: Centralized request routing and handling using dependency injection
- **Automatic Handler Registration**: Automatically discovers and registers all handlers from assemblies
- **Async/Await Support**: Full async support with cancellation tokens
- **Lightweight**: Minimal dependencies (only Microsoft.Extensions.DependencyInjection.Abstractions)
- **.NET 9**: Built for the latest .NET platform with nullable reference types enabled

## Project Structure

```
base-mediatr/
├── Extension/
│   └── ServiceCollectionExtensions.cs  # Automatic handler registration extension
├── Mediator/
│   ├── ICommand.cs          # Command interface definitions (with and without return values)
│   ├── ICommandHandler.cs   # Command handler interfaces
│   ├── IMediator.cs         # Main mediator interface
│   └── IQuery.cs           # Query interface and handler definitions
├── Mediator.cs             # Main mediator implementation
└── base-mediatr.csproj     # Project file
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- Microsoft.Extensions.DependencyInjection (for DI container)

### Installation

Clone the repository:

```bash
git clone <repository-url>
cd base-mediatr
```

Build the project:

```bash
dotnet build
```

### Basic Usage

#### 1. Define Commands and Queries

```csharp
// Command with return value
public record CreateUserCommand(string Name, string Email) : ICommand<int>;

// Command without return value
public record DeleteUserCommand(int UserId) : ICommand;

// Query
public record GetUserQuery(int UserId) : IQuery<User>;
```

#### 2. Implement Handlers

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, int>
{
    public async Task<int> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Create user logic
        return userId;
    }
}

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    public async Task HandleAsync(DeleteUserCommand command, CancellationToken cancellationToken = default)
    {
        // Delete user logic
    }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User>
{
    public async Task<User> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        // Get user logic
        return user;
    }
}
```

#### 3. Register Services

##### Automatic Registration (Recommended)

The easiest way to register the mediator and all handlers is using the extension method:

```csharp
using base_mediatr.Extension;

// Register mediator and automatically discover all handlers in the current assembly
services.AddMediator();

// Or specify specific assemblies to scan
services.AddMediator(typeof(CreateUserCommandHandler).Assembly, typeof(AnotherHandler).Assembly);
```

The `AddMediator()` extension method will:
- Register the `IMediator` service
- Automatically scan assemblies for all handler implementations
- Register all found handlers with the DI container

##### Manual Registration

If you prefer manual registration:

```csharp
services.AddScoped<IMediator, Mediator>();
services.AddScoped<ICommandHandler<CreateUserCommand, int>, CreateUserCommandHandler>();
services.AddScoped<ICommandHandler<DeleteUserCommand>, DeleteUserCommandHandler>();
services.AddScoped<IQueryHandler<GetUserQuery, User>, GetUserQueryHandler>();
```

#### 4. Use the Mediator

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<int> CreateUser(CreateUserCommand command)
    {
        return await _mediator.SendAsync(command);
    }

    [HttpDelete("{id}")]
    public async Task DeleteUser(int id)
    {
        await _mediator.SendAsync(new DeleteUserCommand(id));
    }

    [HttpGet("{id}")]
    public async Task<User> GetUser(int id)
    {
        return await _mediator.SendAsync(new GetUserQuery(id));
    }
}
```

## Key Interfaces

### IMediator
The main interface for sending commands and queries:
- `Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)`
- `Task SendAsync(ICommand command, CancellationToken cancellationToken = default)`
- `Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)`

### Commands
- `ICommand<TResult>`: Commands that return a value
- `ICommand`: Commands that don't return a value
- `ICommandHandler<TCommand, TResult>`: Handler for commands with return values
- `ICommandHandler<TCommand>`: Handler for commands without return values

### Queries
- `IQuery<TResult>`: Query interface that returns data
- `IQueryHandler<TQuery, TResult>`: Handler for queries

## Automatic Handler Registration

The `ServiceCollectionExtensions.AddMediator()` method provides automatic handler discovery and registration:

### Assembly Scanning
- **No parameters**: Scans executing, calling, entry, and all loaded assemblies
- **With assemblies**: Scans only the specified assemblies

### Handler Discovery
The extension automatically finds and registers:
- All classes implementing `ICommandHandler<TCommand, TResult>`
- All classes implementing `ICommandHandler<TCommand>`  
- All classes implementing `IQueryHandler<TQuery, TResult>`

### Registration Scope
All handlers are registered as **Scoped** services, which is appropriate for most web applications and ensures proper lifecycle management.

## Architecture Benefits

- **Separation of Concerns**: Clear separation between business logic and controllers
- **Testability**: Easy to unit test handlers in isolation
- **Maintainability**: Changes to business logic don't affect controllers
- **Scalability**: Easy to add new commands and queries without modifying existing code
- **CQRS**: Natural implementation of Command Query Responsibility Segregation
- **Convention-based**: Automatic registration reduces boilerplate code

## Dependencies

- Microsoft.Extensions.DependencyInjection.Abstractions (9.0.9)

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Inspired by the MediatR library
- Built with .NET 9 and modern C# features
