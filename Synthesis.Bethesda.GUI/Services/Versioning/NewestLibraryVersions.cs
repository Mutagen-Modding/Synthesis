using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;

namespace Synthesis.Bethesda.GUI.Services.Versioning
{
    public interface INewestLibraryVersionsVm
    {
        string? NewestSynthesisVersion { get; }
        string? NewestMutagenVersion { get; }
    }

    public class NewestLibraryVersionsVm : ViewModel, INewestLibraryVersionsVm
    {
        public IQueryNewestLibraryVersions QueryNewest { get; }
        public IInstalledSdkFollower InstalledSdkFollower { get; }
        public IConsiderPrereleasePreference ConsiderPrerelease { get; }

        private readonly ObservableAsPropertyHelper<string?> _NewestSynthesisVersion;
        public string? NewestSynthesisVersion => _NewestSynthesisVersion.Value;

        private readonly ObservableAsPropertyHelper<string?> _NewestMutagenVersion;
        public string? NewestMutagenVersion => _NewestMutagenVersion.Value;

        public NewestLibraryVersionsVm(
            ILogger logger,
            IQueryNewestLibraryVersions queryNewest,
            IInstalledSdkFollower installedSdkFollower,
            IConsiderPrereleasePreference considerPrerelease)
        {
            QueryNewest = queryNewest;
            InstalledSdkFollower = installedSdkFollower;
            ConsiderPrerelease = considerPrerelease;
            var latestVersions = Observable.Return(Unit.Default)
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
                            logger.Error("Can not query for latest nuget versions as there is no acceptable dotnet SDK installed");
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
                .Replay(1)
                .RefCount();
            _NewestMutagenVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.Mutagen : vers.Normal.Mutagen)
                .ToGuiProperty(this, nameof(NewestMutagenVersion), default(string?));
            _NewestSynthesisVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.Synthesis : vers.Normal.Synthesis)
                .ToGuiProperty(this, nameof(NewestSynthesisVersion), default(string?));
        }
    }
}