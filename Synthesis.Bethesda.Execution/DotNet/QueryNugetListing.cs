using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public record NugetListingQuery(string Package, string Requested, string Resolved, string Latest);
    
    public interface IQueryNugetListing
    {
        Task<IEnumerable<NugetListingQuery>> Query(
            FilePath projectPath,
            bool outdated,
            bool includePrerelease,
            CancellationToken cancel);
    }

    public class QueryNugetListing : IQueryNugetListing
    {
        private readonly IProcessFactory _ProcessFactory;
        private readonly IDotNetCommandStartConstructor _dotNetCommandStartConstructor;

        public QueryNugetListing(
            IProcessFactory processFactory,
            IDotNetCommandStartConstructor dotNetCommandStartConstructor)
        {
            _ProcessFactory = processFactory;
            _dotNetCommandStartConstructor = dotNetCommandStartConstructor;
        }
        
        public async Task<IEnumerable<NugetListingQuery>> Query(FilePath projectPath, bool outdated, bool includePrerelease, CancellationToken cancel)
        {
            // Run restore first
            {
                using var restore = _ProcessFactory.Create(
                    _dotNetCommandStartConstructor.Construct("restore", projectPath),
                    cancel: cancel);
                await restore.Run();
            }

            bool on = false;
            List<string> lines = new();
            List<string> errors = new();
            using var process = _ProcessFactory.Create(
                _dotNetCommandStartConstructor.Construct("list",
                    projectPath, 
                    "package",
                    outdated ? " --outdated" : null,
                    includePrerelease ? " --include-prerelease" : null),
                cancel: cancel);
            using var outSub = process.Output.Subscribe(s =>
            {
                if (s.Contains("Top-level Package"))
                {
                    @on = true;
                    return;
                }
                if (!@on) return;
                lines.Add(s);
            });
            using var errSub = process.Error.Subscribe(s => errors.Add(s));
            var result = await process.Run();
            if (errors.Count > 0)
            {
                throw new ArgumentException($"Error while retrieving nuget listings: \n{string.Join("\n", errors)}");
            }

            var ret = new List<NugetListingQuery>();
            foreach (var line in lines)
            {
                if (!DotNetCommands.TryParseLibraryLine(
                    line, 
                    out var package,
                    out var requested, 
                    out var resolved, 
                    out var latest))
                {
                    continue;
                }
                ret.Add(new(package, requested, resolved, latest));
            }
            return ret;
        }
    }
}