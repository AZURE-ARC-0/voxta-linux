using LiteDB;

namespace Voxta.Abstractions.Model;

[Serializable]
public abstract class SettingsBase
{
    [BsonId] public Guid Id { get; set; } = Guid.Empty;
}
