using System.Diagnostics.CodeAnalysis;

namespace ChatMate.Server;

public class SelectorRegistry<TInterface> where TInterface : class
{
    public readonly Dictionary<string, Type> Types = new();

    public void Add<TConcrete>(string key) where TConcrete : class, TInterface
    {
        if (string.IsNullOrEmpty(key) || key == "None") return;
        Types.Add(key, typeof(TConcrete));
    }
}

public class SelectorFactory<TInterface> where TInterface : class
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