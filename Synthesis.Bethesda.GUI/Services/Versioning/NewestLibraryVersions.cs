using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;

namespace Synthesis.Bethesda.GUI.Services.Versioning
{
    public interface INewestLibraryVersions
    {
        IObservable<string?> NewestSynthesisVersion { get; }
        IObservable<string?> NewestMutagenVersion { get; }
    }

    public class NewestLibraryVersions : INewestLibraryVersions
    {
        public IQueryNewestLibraryVersions QueryNewest { get; }
        public IInstalledSdkFollower InstalledSdkFollower { get; }
        public IConsiderPrereleasePreference ConsiderPrerelease { get; }
        public IObservable<string?> NewestSynthesisVersion { get; }
        public IObservable<string?> NewestMutagenVersion { get; }

        public NewestLibraryVersions(
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

                        return await queryNewest.GetLatestVersions(CancellationToken.None);
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
            NewestMutagenVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.Mutagen : vers.Normal.Mutagen)
                .Replay(1)
                .RefCount();
            NewestSynthesisVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.Synthesis : vers.Normal.Synthesis)
                .Replay(1)
                .RefCount();
        }
    }
}