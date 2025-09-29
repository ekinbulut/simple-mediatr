using System.Reflection;
using base_mediatr;
using base_mediatr.Extension;
using Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_WithoutAssembly_RegistersHandlersFromCallingAssembly()
    {
        var services = new ServiceCollection();
        
        services.AddMediator();
        var serviceProvider = services.BuildServiceProvider();
        
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddMediator_WithSpecificAssembly_RegistersHandlersFromThatAssembly()
    {
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();
        
        services.AddMediator(assembly);
        var serviceProvider = services.BuildServiceProvider();
        
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddMediator_WithMultipleAssemblies_RegistersHandlersFromAllAssemblies()
    {
        var services = new ServiceCollection();
        var assembly1 = Assembly.GetExecutingAssembly();
        var assembly2 = typeof(string).Assembly;
        
        services.AddMediator(assembly1, assembly2);
        var serviceProvider = services.BuildServiceProvider();
        
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddMediator_WithEmptyAssemblyArray_RegistersHandlersFromCallingAssembly()
    {
        var services = new ServiceCollection();
        
        services.AddMediator(new Assembly[0]);
        var serviceProvider = services.BuildServiceProvider();
        
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public void AddMediator_WithNullAssemblyArray_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        
        Assert.Throws<ArgumentNullException>(() => services.AddMediator((Assembly[])null));
    }

    [Fact]
    public void AddMediator_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        IServiceCollection services = null;
        
        Assert.Throws<ArgumentNullException>(() => services.AddMediator());
    }

    [Fact]
    public void AddMediator_RegistersCommandHandlers_AsScoped()
    {
        var services = new ServiceCollection();
        
        services.AddMediator();
        
        var commandHandlerDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
        
        Assert.Equal(ServiceLifetime.Scoped, commandHandlerDescriptor?.Lifetime);
    }

    [Fact]
    public void AddMediator_RegistersCommandHandlersWithResponse_AsScoped()
    {
        var services = new ServiceCollection();
        
        services.AddMediator();
        
        var commandHandlerDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
        
        Assert.Equal(ServiceLifetime.Scoped, commandHandlerDescriptor?.Lifetime);
    }

    [Fact]
    public void AddMediator_RegistersQueryHandlers_AsScoped()
    {
        var services = new ServiceCollection();
        
        services.AddMediator();
        
        var queryHandlerDescriptor = services.FirstOrDefault(s => 
            s.ServiceType.IsGenericType && 
            s.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
        
        Assert.Equal(ServiceLifetime.Scoped, queryHandlerDescriptor?.Lifetime);
    }
    
    [Fact]
    public void AddMediator_RegistersMediator_AsScoped()
    {
        var services = new ServiceCollection();

        services.AddMediator();

        var mediatorDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IMediator));
        Assert.Equal(ServiceLifetime.Scoped, mediatorDescriptor?.Lifetime);
    }

}