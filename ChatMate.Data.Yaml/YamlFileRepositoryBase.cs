using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace ChatMate.Data.Yaml;

public abstract class YamlFileRepositoryBase
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .Build();
    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .Build();

    protected static async Task<T> DeserializeFileAsync<T>(string file)
    {
        await using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);
        try
        {
            var obj = YamlDeserializer.Deserialize<T>(reader);
            return obj;
        }
        catch (SemanticErrorException exc)
        {
            throw new SemanticErrorException($"Failed to deserialize YAML file: {file}", exc);
        }
    }
    
    protected static async Task SerializeFileAsync<T>(string file, T value)
    {
        await using var stream = File.Create(file);
        await using var reader = new StreamWriter(stream);
        YamlSerializer.Serialize(reader, value);
    }
}