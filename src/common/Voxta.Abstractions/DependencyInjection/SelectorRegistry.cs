namespace Voxta.Abstractions.DependencyInjection;

public interface IServiceRegistry<in TInterface> where TInterface : class
{
    Dictionary<string, Type> Types { get; }
    void Add<TConcrete>(string key) where TConcrete : class, TInterface;
}

public class ServiceRegistry<TInterface> : IServiceRegistry<TInterface> where TInterface : class
{
    public Dictionary<string, Type> Types { get; } = new();

    public void Add<TConcrete>(string key) where TConcrete : class, TInterface
    {
        if (string.IsNullOrEmpty(key) || key == "None") return;
        Types.Add(key, typeof(TConcrete));
    }
}
