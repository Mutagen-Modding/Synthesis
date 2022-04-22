using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
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
    public IQueryVersionProjectPathing Pathing { get; }
    public IPrepLatestVersionProject PrepLatestVersionProject { get; }
    public IQueryLibraryVersions QueryLibraryVersions { get; }

    public QueryNewestLibraryVersions(
        ILogger logger,
        IQueryVersionProjectPathing pathing, 
        IWorkDropoff workDropoff,
        IPrepLatestVersionProject prepLatestVersionProject,
        IQueryLibraryVersions queryLibraryVersions)
    {
        _logger = logger;
        _workDropoff = workDropoff;
        Pathing = pathing;
        PrepLatestVersionProject = prepLatestVersionProject;
        QueryLibraryVersions = queryLibraryVersions;
    }

    public async Task<NugetVersionOptions> GetLatestVersions(CancellationToken cancel)
    {
        try
        {
            _logger.Information("Querying for latest published library versions");
            await _workDropoff.EnqueueAndWait(() => PrepLatestVersionProject.Prep(cancel), cancel);

            var normalTask = _workDropoff.EnqueueAndWait(() =>
                QueryLibraryVersions.Query(
                    Pathing.ProjectFile,
                    current: false,
                    includePrerelease: false,
                    cancel), cancel);
            var prereleaseTask = _workDropoff.EnqueueAndWait(() =>
                QueryLibraryVersions.Query(
                    Pathing.ProjectFile,
                    current: false,
                    includePrerelease: true,
                    cancel), cancel);

            var normal = await normalTask.ConfigureAwait(false);
            var prerelease = await prereleaseTask.ConfigureAwait(false);
                
            _logger.Information("Latest published library versions:");
            _logger.Information("  Mutagen: {MutagenVersion}", normal.Mutagen);
            _logger.Information("  Synthesis: {SynthesisVersion}", normal.Synthesis);
            _logger.Information("Latest published prerelease library versions:");
            _logger.Information("  Mutagen: {MutagenVersion}", prerelease.Mutagen);
            _logger.Information("  Synthesis: {SynthesisVersion}", prerelease.Synthesis);
                
            return new NugetVersionOptions(normal, prerelease);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error querying for latest nuget versions");
            return new(
                new NugetVersionPair(null, null),
                new NugetVersionPair(null, null));
        }
    }
}