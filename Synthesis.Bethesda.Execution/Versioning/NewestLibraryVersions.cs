using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.Execution.Versioning
{
    public interface INewestLibraryVersions
    {
        IObservable<string?> NewestSynthesisVersion { get; }
        IObservable<string?> NewestMutagenVersion { get; }
    }

    public class NewestLibraryVersions : INewestLibraryVersions
    {
        public IQueryNewestLibraryVersions QueryNewest { get; }
        public IInstalledSdkProvider InstalledSdkProvider { get; }
        public IConsiderPrereleasePreference ConsiderPrerelease { get; }
        public IObservable<string?> NewestSynthesisVersion { get; }
        public IObservable<string?> NewestMutagenVersion { get; }

        public NewestLibraryVersions(
            ILogger logger,
            IQueryNewestLibraryVersions queryNewest,
            IInstalledSdkProvider installedSdkProvider,
            IConsiderPrereleasePreference considerPrerelease)
        {
            QueryNewest = queryNewest;
            InstalledSdkProvider = installedSdkProvider;
            ConsiderPrerelease = considerPrerelease;
            var latestVersions = Observable.Return(Unit.Default)
                .ObserveOn(TaskPoolScheduler.Default)
                .CombineLatest(
                    installedSdkProvider.DotNetSdkInstalled,
                    (_, DotNetVersions) => DotNetVersions)
                .SelectTask(async x =>
                {
                    try
                    {
                        if (!x.Acceptable)
                        {
                            logger.Error("Can not query for latest nuget versions as there is no acceptable dotnet SDK installed.");
                            return (Normal: (MutagenVersion: default(string?), SynthesisVersion: default(string?)), Prerelease: (MutagenVersion: default(string?), SynthesisVersion: default(string?)));
                        }

                        var projPath = queryNewest.PrepLatestVersionProject();
                    
                        logger.Information("Querying for latest published library versions");
                        var normalUpdate = await queryNewest.GetLatestVersions(includePrerelease: false, projPath);
                        var prereleaseUpdate = await queryNewest.GetLatestVersions(includePrerelease: true, projPath);
                        return (Normal: normalUpdate, Prerelease: prereleaseUpdate);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error querying for versions: {e}");
                        return (Normal: (MutagenVersion: default(string?), SynthesisVersion: default(string?)), Prerelease: (MutagenVersion: default(string?), SynthesisVersion: default(string?)));
                    }
                })
                .Replay(1)
                .RefCount();
            NewestMutagenVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.MutagenVersion : vers.Normal.MutagenVersion)
                .Replay(1)
                .RefCount();
            NewestSynthesisVersion = Observable.CombineLatest(
                    latestVersions,
                    considerPrerelease.ConsiderPrereleases,
                    (vers, prereleases) => prereleases ? vers.Prerelease.SynthesisVersion : vers.Normal.SynthesisVersion)
                .Replay(1)
                .RefCount();
        }
    }
}