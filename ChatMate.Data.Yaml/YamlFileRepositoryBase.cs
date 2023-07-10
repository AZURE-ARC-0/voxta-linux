using System.Diagnostics.CodeAnalysis;
using System.Security;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace ChatMate.Data.Yaml;

public abstract class YamlFileRepositoryBase
{
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidFileNameChars());

    protected static void IsValidFileName([NotNull] string? filename)
    {
        if (filename == null) throw new ArgumentNullException(nameof(filename));
        if (filename.Any(InvalidFileNameChars.Contains) || filename.Equals(".") || filename.Contains(".."))
            throw new SecurityException($"Invalid filename: {filename}");
        if (!filename.EndsWith(".yaml"))
            throw new SecurityException($"File name '{filename}' must end with .yaml");
    }

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .Build();
    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .Build();

    protected static async Task<T?> DeserializeFileAsync<T>(string file) where T : class
    {
        if (!File.Exists(file)) return null;
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
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? throw new InvalidOperationException("Invalid file path"));
        await using var stream = File.Create(file);
        await using var reader = new StreamWriter(stream);
        YamlSerializer.Serialize(reader, value);
    }
}