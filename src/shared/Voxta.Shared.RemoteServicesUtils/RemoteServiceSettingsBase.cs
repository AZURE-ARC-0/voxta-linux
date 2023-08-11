using Voxta.Abstractions.Repositories;

namespace Voxta.Shared.RemoteServicesUtils;

[Serializable]
public class RemoteServiceSettingsBase<TParameters> : SettingsBase
{
    public required string Uri { get; set; }
    public int MaxMemoryTokens { get; set; } = 400;
    public int MaxContextTokens { get; set; } = 1600;
    public TParameters? Parameters { get; set; }
}