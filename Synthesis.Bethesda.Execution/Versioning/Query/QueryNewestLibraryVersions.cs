using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.Execution.Versioning.Query
{
    public interface IQueryNewestLibraryVersions
    {
        Task<NugetVersionOptions> GetLatestVersions(CancellationToken cancel);
    }

    public class QueryNewestLibraryVersions : IQueryNewestLibraryVersions
    {
        private readonly ILogger _logger;
        public IQueryVersionProjectPathing Pathing { get; }
        public IPrepLatestVersionProject PrepLatestVersionProject { get; }
        public IQueryLibraryVersions QueryLibraryVersions { get; }

        public QueryNewestLibraryVersions(
            ILogger logger,
            IQueryVersionProjectPathing pathing, 
            IPrepLatestVersionProject prepLatestVersionProject,
            IQueryLibraryVersions queryLibraryVersions)
        {
            _logger = logger;
            Pathing = pathing;
            PrepLatestVersionProject = prepLatestVersionProject;
            QueryLibraryVersions = queryLibraryVersions;
        }

        public async Task<NugetVersionOptions> GetLatestVersions(CancellationToken cancel)
        {
            try
            {
                _logger.Information("Querying for latest published library versions");
                PrepLatestVersionProject.Prep();
                return new NugetVersionOptions(
                    await Task.Run(async () =>
                    {
                        var ret = await QueryLibraryVersions.Query(
                            Pathing.ProjectFile, 
                            current: false,
                            includePrerelease: false, 
                            cancel).ConfigureAwait(false);
                        _logger.Information("Latest published library versions:");
                        _logger.Information("  Mutagen: {MutagenVersion}", ret.Mutagen);
                        _logger.Information("  Synthesis: {SynthesisVersion}", ret.Synthesis);
                        return ret;
                    }).ConfigureAwait(false),
                    await Task.Run(async () =>
                    {
                        var ret = await QueryLibraryVersions.Query(
                            Pathing.ProjectFile, 
                            current: false,
                            includePrerelease: true,
                            cancel).ConfigureAwait(false);
                        _logger.Information("Latest published prerelease library versions:");
                        _logger.Information("  Mutagen: {MutagenVersion}", ret.Mutagen);
                        _logger.Information("  Synthesis: {SynthesisVersion}", ret.Synthesis);
                        return ret;
                    }).ConfigureAwait(false));
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
}