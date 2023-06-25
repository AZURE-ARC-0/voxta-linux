using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace ChatMate.Abstractions.DependencyInjection;

public interface ISelectorRegistry<TInterface> where TInterface : class
{
    void Add<TConcrete>(string key) where TConcrete : class, TInterface;
}

public class SelectorRegistry<TInterface> : ISelectorRegistry<TInterface> where TInterface : class
{
    public readonly Dictionary<string, Type> Types = new();

    public void Add<TConcrete>(string key) where TConcrete : class, TInterface
    {
        if (string.IsNullOrEmpty(key) || key == "None") return;
        Types.Add(key, typeof(TConcrete));
    }
}

public interface ISelectorFactory<TInterface> where TInterface : class
{
    TInterface Create(string key);
    bool TryCreate(string? key, [NotNullWhen(true)] out TInterface? value);
}

public class SelectorFactory<TInterface> : ISelectorFactory<TInterface> where TInterface : class
{
    private readonly SelectorRegistry<TInterface> _registry;
    private readonly IServiceProvider _sp;

    public SelectorFactory(SelectorRegistry<TInterface> registry, IServiceProvider sp)
    {
        _registry = registry;
        _sp = sp;
    }

    public TInterface Create(string key)
    {
        return (TInterface)_sp.GetRequiredService(_registry.Types[key]);
    }

    public bool TryCreate(string? key, [NotNullWhen(true)] out TInterface? value)
    {
        if (string.IsNullOrEmpty(key) || key == "None" || !_registry.Types.TryGetValue(key, out var type))
        {
            value = null;
            return false;
        }
        value = (TInterface)_sp.GetRequiredService(type);
        return true;
    }
}