using LiteDB;
using Voxta.Abstractions.Repositories;

namespace Voxta.Abstractions.Model;

[Serializable]
public class ConfiguredService
{
    [BsonId] public required Guid Id { get; init; } = Guid.Empty;
    public string Label { get; init; } = "";
    public required string ServiceName { get; set; } = null!;
    public bool Enabled { get; set; } = true;
}

[Serializable]
public class ConfiguredService<TSettings> : ConfiguredService
    where TSettings : SettingsBase
{
    public required TSettings Settings { get; set; }
}
