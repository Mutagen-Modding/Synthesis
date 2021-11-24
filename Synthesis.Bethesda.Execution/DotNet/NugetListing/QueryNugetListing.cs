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
        public IProcessNugetQueryResults ResultProcessor { get; }
        public IProcessFactory ProcessFactory { get; }
        public IProcessRunner ProcessRunner { get; }
        public IDotNetCommandStartConstructor NetCommandStartConstructor { get; }

        public QueryNugetListing(
            IProcessFactory processFactory,
            IProcessRunner processRunner,
            IProcessNugetQueryResults resultProcessor,
            IDotNetCommandStartConstructor dotNetCommandStartConstructor)
        {
            ResultProcessor = resultProcessor;
            ProcessFactory = processFactory;
            ProcessRunner = processRunner;
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

            return ResultProcessor.Process(result.Out);
        }
    }
}