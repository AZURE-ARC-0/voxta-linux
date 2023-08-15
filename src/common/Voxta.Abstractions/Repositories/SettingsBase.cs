using LiteDB;

namespace Voxta.Abstractions.Repositories;

[Serializable]
public abstract class SettingsBase
{
    public static readonly string SharedId = Guid.Empty.ToString();
    
    [BsonId] public string Id { get; init; } = SharedId;

    public bool Enabled { get; set; } = true;
}