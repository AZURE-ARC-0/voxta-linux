using Voxta.Abstractions;
using Voxta.Abstractions.Model;
using Voxta.Abstractions.Repositories;
using Voxta.Abstractions.Services;

namespace Voxta.Services.Mocks;

public class MockServiceBase : ServiceBase<MockSettings>
{
    protected override string ServiceName => MockConstants.ServiceName;
    public string[] Features => new[] { ServiceFeatures.NSFW };

    public MockServiceBase(ISettingsRepository settingsRepository)
        : base(settingsRepository)
    {
    }

    protected override async Task<bool> TryInitializeAsync(MockSettings settings, IPrerequisitesValidator prerequisites, string culture, bool dry,
        CancellationToken cancellationToken)
    {
        if (!await base.TryInitializeAsync(settings, prerequisites, culture, dry, cancellationToken)) return false;
        
        return true;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}