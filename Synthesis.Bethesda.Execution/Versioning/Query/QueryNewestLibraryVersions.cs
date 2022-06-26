using System.IO.Abstractions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Projects;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.Execution.Versioning.Query;

public interface IQueryNewestLibraryVersions
{
    Task<NugetVersionOptions> GetLatestVersions(CancellationToken cancel);
}

public class QueryNewestLibraryVersions : IQueryNewestLibraryVersions
{
    private readonly ILogger _logger;
    private readonly IWorkDropoff _workDropoff;
    private readonly IProcessRunner _processRunner;
    private readonly IDotNetCommandStartConstructor _dotNetCommandStartConstructor;
    public IQueryVersionProjectPathing Pathing { get; }
    public IQueryLibraryVersions QueryLibraryVersions { get; }
    public IFileSystem FileSystem { get; }
    public ICreateSolutionFile CreateSolutionFile { get; }
    public ICreateProject CreateProject { get; }
    public IDeleteEntireDirectory DeleteEntireDirectory { get; }
    public IAddProjectToSolution AddProjectToSolution { get; }

    public QueryNewestLibraryVersions(
        ILogger logger,
        IFileSystem fileSystem,
        ICreateSolutionFile createSolutionFile,
        ICreateProject createProject,
        IQueryVersionProjectPathing pathing, 
        IDeleteEntireDirectory deleteEntireDirectory,
        IAddProjectToSolution addProjectToSolution,
        IWorkDropoff workDropoff,
        IProcessRunner processRunner,
        IDotNetCommandStartConstructor dotNetCommandStartConstructor,
        IQueryLibraryVersions queryLibraryVersions)
    {
        _logger = logger;
        _workDropoff = workDropoff;
        _processRunner = processRunner;
        _dotNetCommandStartConstructor = dotNetCommandStartConstructor;
        FileSystem = fileSystem;
        CreateSolutionFile = createSolutionFile;
        CreateProject = createProject;
        Pathing = pathing;
        DeleteEntireDirectory = deleteEntireDirectory;
        AddProjectToSolution = addProjectToSolution;
        QueryLibraryVersions = queryLibraryVersions;
    }

    public async Task<NugetVersionOptions> GetLatestVersions(CancellationToken cancel)
    {
        return await _workDropoff.EnqueueAndWait(async () =>
        {
            try
            {
                _logger.Information("Querying for latest published library versions");
                if (!Exists())
                {
                    await Create(cancel);
                }

                NugetVersionOptions versions;
                try
                {
                    versions = await GetVersionsWithRestore(cancel);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error querying for latest nuget versions.  Purging and trying again");
                    await Reset(cancel);
                    versions = await GetVersionsWithRestore(cancel);
                }

                Print(versions);
                return versions;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error querying for latest nuget versions");
                return new(
                    new NugetVersionPair(null, null),
                    new NugetVersionPair(null, null));
            }
        }, cancel);
    }

    private bool Exists() => FileSystem.Directory.Exists(Pathing.BaseFolder);

    private async Task Reset(CancellationToken cancel)
    {
        _logger.Information("Deleting Version Query Project");
        DeleteEntireDirectory.DeleteEntireFolder(Pathing.BaseFolder);
        await Create(cancel);
    }
    
    private async Task Create(CancellationToken cancel)
    {
        _logger.Information("Creating Version Query Project");
        FileSystem.Directory.CreateDirectory(Pathing.BaseFolder);
        CreateSolutionFile.Create(Pathing.SolutionFile);
        CreateProject.Create(GameCategory.Skyrim, Pathing.ProjectFile, insertOldVersion: true,
            targetFramework: "net5.0");
        AddProjectToSolution.Add(Pathing.SolutionFile, Pathing.ProjectFile);

        await Restore(cancel);
    }
    
    private async Task Restore(CancellationToken cancel)
    {
        _logger.Information("Restoring Version Query Project");
        var result = await _processRunner.RunAndCapture(
            _dotNetCommandStartConstructor.Construct("restore", Pathing.ProjectFile),
            cancel: cancel).ConfigureAwait(false);
        if (result.Result != 0)
        {
            _logger.Error("Failed to restore Version Query Project: {Result}", result.Result);
            foreach (var err in result.Out)
            {
                _logger.Error(err);
            }
            foreach (var err in result.Errors)
            {
                _logger.Error(err);
            }

            throw new Exception("Failed to restore Version Query Project");
        }
        else
        {
            _logger.Information("Restored Version Query Project");
        }
    }

    private async Task<NugetVersionOptions> GetVersions(CancellationToken cancel)
    {
        var normalTask = QueryLibraryVersions.Query(
            Pathing.ProjectFile,
            current: false,
            includePrerelease: false,
            cancel);
        var prereleaseTask = QueryLibraryVersions.Query(
            Pathing.ProjectFile,
            current: false,
            includePrerelease: true,
            cancel);

        var normal = await normalTask.ConfigureAwait(false);
        var prerelease = await prereleaseTask.ConfigureAwait(false);
        return new(normal, prerelease);
    }

    private async Task<NugetVersionOptions> GetVersionsWithRestore(CancellationToken cancel)
    {
        await Restore(cancel);
        return await GetVersions(cancel);
    }

    private void Print(NugetVersionOptions versions)
    {
        _logger.Information("Latest published library versions:");
        _logger.Information("  Mutagen: {MutagenVersion}", versions.Normal.Mutagen);
        _logger.Information("  Synthesis: {SynthesisVersion}", versions.Normal.Synthesis);
        _logger.Information("Latest published prerelease library versions:");
        _logger.Information("  Mutagen: {MutagenVersion}", versions.Prerelease.Mutagen);
        _logger.Information("  Synthesis: {SynthesisVersion}", versions.Prerelease.Synthesis);
    }
}