using LiteDB;

namespace Voxta.Abstractions.Repositories;

[Serializable]
public abstract class SettingsBase
{
    [BsonId] public Guid Id { get; set; } = Guid.Empty;
}
