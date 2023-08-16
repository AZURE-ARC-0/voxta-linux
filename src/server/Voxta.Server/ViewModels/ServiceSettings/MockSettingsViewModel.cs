using System.Diagnostics.CodeAnalysis;
using Voxta.Abstractions.Model;
using Voxta.Services.Mocks;

namespace Voxta.Server.ViewModels.ServiceSettings;

public class MockSettingsViewModel : ServiceSettingsViewModel
{
    public MockSettingsViewModel()
    {
    }

    [SetsRequiredMembers]
    public MockSettingsViewModel(ConfiguredService<MockSettings> source)
        : base(source)
    {
    }

    public ConfiguredService<MockSettings> ToSettings(Guid serviceId)
    {
        return new ConfiguredService<MockSettings>
        {
            Id = serviceId,
            ServiceName = MockConstants.ServiceName,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label,
            Enabled = Enabled,
            Settings = new MockSettings(),
        };
    }
}