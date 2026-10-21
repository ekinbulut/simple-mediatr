using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Smpl.Mediatr.Extension;

public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerDefinitions =
    {
        typeof(ICommandHandler<,>),
        typeof(ICommandHandler<>),
        typeof(IQueryHandler<,>),
        typeof(INotificationHandler<>)
    };

    /// <summary>
    /// Scans the calling assembly and every loaded assembly. Prefer the overload that takes assemblies explicitly.
    /// </summary>
    [Obsolete("Pass the assemblies to scan explicitly: AddMediator(typeof(Marker).Assembly).")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        return AddMediatorCore(services, ScanAllAssemblies(Assembly.GetCallingAssembly()));
    }

    /// <summary>
    /// Registers <see cref="IMediator"/> and every command, query and notification handler found in the assemblies.
    /// Pipeline behaviors are not scanned; register them yourself (first registered = outermost).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies.Any()
            ? assemblies
            : ScanAllAssemblies(Assembly.GetCallingAssembly());
        return AddMediatorCore(services, assembliesToScan);
    }

    private static Assembly[] ScanAllAssemblies(Assembly caller)
    {
        return new[] { Assembly.GetExecutingAssembly(), caller, Assembly.GetEntryAssembly() }
            .Where(a => a != null)
            .Select(a => a!)
            .Concat(AppDomain.CurrentDomain.GetAssemblies())
            .Distinct()
            .ToArray();
    }

    private static IServiceCollection AddMediatorCore(IServiceCollection services, Assembly[] assemblies)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        foreach (var assembly in assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition);

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && HandlerDefinitions.Contains(i.GetGenericTypeDefinition()));

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, handlerType);
            }
        }
    }
}
