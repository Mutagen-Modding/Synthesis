using System.Reactive.Linq;
using Noggog.Reactive;
using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for DotNetSdkOutdatedErrorClassification.
/// Surfaces the targeted .NET version and lists the SDKs currently installed on the machine.
/// </summary>
public class DotNetSdkOutdatedErrorVm : ErrorClassificationVm
{
    public string? TargetVersion { get; }
    public bool HasTargetVersion => TargetVersion != null;

    private readonly ObservableAsPropertyHelper<IReadOnlyList<string>> _installedSdks;
    public IReadOnlyList<string> InstalledSdks => _installedSdks.Value;

    private readonly ObservableAsPropertyHelper<bool> _hasInstalledSdks;
    public bool HasInstalledSdks => _hasInstalledSdks.Value;

    public delegate DotNetSdkOutdatedErrorVm Factory(DotNetSdkOutdatedErrorClassification error);

    public DotNetSdkOutdatedErrorVm(
        DotNetSdkOutdatedErrorClassification error,
        INavigateTo navigateTo,
        IQueryInstalledSdkListings installedSdkListings,
        ISchedulerProvider schedulerProvider)
        : base(error, navigateTo)
    {
        TargetVersion = error.TargetVersion;

        var listings = Observable.FromAsync(() => installedSdkListings.Query(CancellationToken.None))
            .SubscribeOn(schedulerProvider.TaskPool)
            .Catch((Exception _) => Observable.Return<IReadOnlyList<string>>(Array.Empty<string>()))
            .Replay(1)
            .RefCount();

        _installedSdks = listings
            .ToGuiProperty(this, nameof(InstalledSdks), (IReadOnlyList<string>)Array.Empty<string>(),
                schedulerProvider.MainThread, deferSubscription: true);

        _hasInstalledSdks = listings
            .Select(x => x.Count > 0)
            .ToGuiProperty(this, nameof(HasInstalledSdks), false, schedulerProvider.MainThread, deferSubscription: true);
    }
}
