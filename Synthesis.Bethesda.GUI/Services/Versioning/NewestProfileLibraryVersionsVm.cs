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
    public interface INewestProfileLibraryVersionsVm
    {
        string? NewestSynthesisVersion { get; }
        string? NewestMutagenVersion { get; }
    }

    public class NewestProfileLibraryVersionsVm : ViewModel, INewestProfileLibraryVersionsVm
    {
        public IQueryNewestLibraryVersions QueryNewest { get; }
        public IInstalledSdkFollower InstalledSdkFollower { get; }
        public IConsiderPrereleasePreference ConsiderPrerelease { get; }

        private readonly ObservableAsPropertyHelper<string?> _newestSynthesisVersion;
        public string? NewestSynthesisVersion => _newestSynthesisVersion.Value;

        private readonly ObservableAsPropertyHelper<string?> _newestMutagenVersion;
        public string? NewestMutagenVersion => _newestMutagenVersion.Value;

        public NewestProfileLibraryVersionsVm(
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
            _newestMutagenVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.Mutagen : vers.Normal.Mutagen)
                .ToGuiProperty(this, nameof(NewestMutagenVersion), default(string?));
            _newestSynthesisVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.Synthesis : vers.Normal.Synthesis)
                .ToGuiProperty(this, nameof(NewestSynthesisVersion), default(string?));
        }
    }
}