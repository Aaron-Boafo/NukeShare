using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace NukeShare.CLI.Infrastructure;

public sealed class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    private readonly IServiceCollection _services = services;

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _services.AddSingleton(service, _ => factory());
    }
}

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    private readonly IServiceProvider _provider = provider;

    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        return _provider.GetService(type);
    }
}



