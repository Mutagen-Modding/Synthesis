using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Singleton;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;

namespace Synthesis.Bethesda.GUI.Services.Versioning;

public interface INewestLibraryVersionsVm
{
    NugetVersionOptions Versions { get; }
}
    
public class NewestLibraryVersionsVm : ViewModel, INewestLibraryVersionsVm
{
    private readonly ObservableAsPropertyHelper<NugetVersionOptions> _versions;
    public NugetVersionOptions Versions => _versions.Value;

    public NewestLibraryVersionsVm(
        ILogger logger,
        IQueryNewestLibraryVersions queryNewest,
        IInstalledSdkFollower installedSdkFollower)
    {
        _versions = Observable.Return(Unit.Default)
            .ObserveOn(TaskPoolScheduler.Default)
            .CombineLatest(
                installedSdkFollower.DotNetSdkInstalled,
                (_, DotNetVersions) => DotNetVersions)
            .SelectTask(async x =>
            {
                try
                {
                    if (!x.Acceptable)
                    {
                        logger.Error(
                            "Can not query for latest nuget versions as there is no acceptable dotnet SDK installed");
                        return new NugetVersionOptions(
                            new NugetVersionPair(null, null),
                            new NugetVersionPair(null, null));
                    }

                    return await queryNewest.GetLatestVersions(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error querying for versions");
                    return new NugetVersionOptions(
                        new NugetVersionPair(null, null),
                        new NugetVersionPair(null, null));
                }
            })
            .ToGuiProperty(this, nameof(Versions), new NugetVersionOptions(
                new NugetVersionPair(null, null),
                new NugetVersionPair(null, null)), deferSubscription: true);
    }
}