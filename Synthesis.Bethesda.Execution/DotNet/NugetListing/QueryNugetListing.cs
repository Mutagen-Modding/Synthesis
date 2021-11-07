using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet.NugetListing
{
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
        public const string Delimeter = "Top-level Package";
        public IProcessFactory ProcessFactory { get; }
        public IProcessRunner ProcessRunner { get; }
        public INugetListingParser LineParser { get; }
        public IDotNetCommandStartConstructor NetCommandStartConstructor { get; }

        public QueryNugetListing(
            IProcessFactory processFactory,
            IProcessRunner processRunner,
            INugetListingParser lineParser,
            IDotNetCommandStartConstructor dotNetCommandStartConstructor)
        {
            ProcessFactory = processFactory;
            ProcessRunner = processRunner;
            LineParser = lineParser;
            NetCommandStartConstructor = dotNetCommandStartConstructor;
        }
        
        public async Task<IEnumerable<NugetListingQuery>> Query(FilePath projectPath, bool outdated, bool includePrerelease, CancellationToken cancel)
        {
            // Run restore first
            await ProcessRunner.Run(
                NetCommandStartConstructor.Construct("restore", projectPath),
                cancel: cancel).ConfigureAwait(false);

            var result = await ProcessRunner.RunAndCapture(
                NetCommandStartConstructor.Construct("list",
                    projectPath, 
                    "package",
                    outdated ? "--outdated" : null,
                    includePrerelease ? "--include-prerelease" : null),
                cancel: cancel).ConfigureAwait(false);
            if (result.Errors.Count > 0)
            {
                throw new InvalidOperationException($"Error while retrieving nuget listings: \n{string.Join("\n", result.Errors)}");
            }
            
            bool on = false;
            var lines = new List<string>();
            foreach (var s in result.Out)
            {
                if (s.Contains(Delimeter))
                {
                    on = true;
                    continue;
                }
                if (!on) continue;
                lines.Add(s);
            }

            var ret = new List<NugetListingQuery>();
            foreach (var line in lines)
            {
                if (!LineParser.TryParse(
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