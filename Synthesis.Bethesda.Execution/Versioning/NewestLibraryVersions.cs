using System;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Path = System.IO.Path;

namespace Synthesis.Bethesda.Execution.Versioning
{
    public interface INewestLibraryVersions
    {
        IObservable<string?> NewestSynthesisVersion { get; }
        IObservable<string?> NewestMutagenVersion { get; }
    }

    public class NewestLibraryVersions : INewestLibraryVersions
    {
        private readonly IFileSystem _FileSystem;
        private readonly IProvideCurrentVersions _CurrentVersions;
        private readonly IQueryLibraryVersions _QueryLibraryVersions;
        
        public IObservable<string?> NewestSynthesisVersion { get; }
        public IObservable<string?> NewestMutagenVersion { get; }

        public NewestLibraryVersions(
            ILogger logger,
            IFileSystem fileSystem,
            IProvideCurrentVersions currentVersions,
            IProvideInstalledSdk installedSdk,
            IQueryLibraryVersions queryLibraryVersions,
            IConsiderPrereleasePreference considerPrerelease)
        {
            _FileSystem = fileSystem;
            _CurrentVersions = currentVersions;
            _QueryLibraryVersions = queryLibraryVersions;
            var latestVersions = Observable.Return(Unit.Default)
                .ObserveOn(TaskPoolScheduler.Default)
                .CombineLatest(
                    installedSdk.DotNetSdkInstalled,
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

                        var projPath = PrepLatestVersionProject();
                    
                        logger.Information("Querying for latest published library versions");
                        var normalUpdate = await GetLatestVersions(includePrerelease: false, projPath);
                        var prereleaseUpdate = await GetLatestVersions(includePrerelease: true, projPath);
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
        
        public string PrepLatestVersionProject()
        {
            var bootstrapProjectDir = new DirectoryPath(Path.Combine(Execution.Paths.WorkingDirectory, "VersionQuery"));
            bootstrapProjectDir.DeleteEntireFolder();
            bootstrapProjectDir.Create();
            var slnPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.sln");
            SolutionInitialization.CreateSolutionFile(_FileSystem, slnPath);
            var projPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.csproj");
            SolutionInitialization.CreateProject(_FileSystem, projPath, GameCategory.Skyrim, insertOldVersion: true);
            SolutionInitialization.AddProjectToSolution(_FileSystem, slnPath, projPath);
            return projPath;
        }

        public async Task<(string? MutagenVersion, string? SynthesisVersion)> GetLatestVersions(bool includePrerelease, string projPath)
        {
            try
            {
                var ret = await _QueryLibraryVersions.Query(projPath, current: false, includePrerelease: includePrerelease, CancellationToken.None);
                Log.Logger.Information($"Latest published {(includePrerelease ? " prerelease" : null)} library versions:");
                Log.Logger.Information($"  Mutagen: {ret.MutagenVersion}");
                Log.Logger.Information($"  Synthesis: {ret.SynthesisVersion}");
                return (ret.MutagenVersion ?? _CurrentVersions.MutagenVersion, ret.SynthesisVersion ?? _CurrentVersions.SynthesisVersion);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error querying for latest nuget versions");
                return (null, null);
            }
        }
    }
}