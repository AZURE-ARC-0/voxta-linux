using Voxta.Abstractions.Services;

namespace Voxta.Server.ViewModels.Settings;

public class AddServiceViewModel
{
    public required ServiceEntryViewModel[] Services { get; init; }

    public class ServiceEntryViewModel
    {
        public required ServiceHelp Help { get; init; }
        public required int Occurrences { get; init; }
        public required int EnabledOccurrences { get; init; }
    }
}
