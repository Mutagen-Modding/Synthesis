using System;
using System.Threading;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IQueryInstalledSdk
    {
        Task<DotNetVersion> Query(CancellationToken cancel);
    }

    public class QueryInstalledSdk : IQueryInstalledSdk
    {
        public IProcessRunner ProcessRunner { get; }
        public IDotNetCommandPathProvider NetCommandPathProvider { get; }
        public IParseNugetVersionString NugetVersionString { get; }

        public QueryInstalledSdk(
            IDotNetCommandPathProvider dotNetCommandPathProvider,
            IParseNugetVersionString parseNugetVersionString,
            IProcessRunner processRunner)
        {
            ProcessRunner = processRunner;
            NetCommandPathProvider = dotNetCommandPathProvider;
            NugetVersionString = parseNugetVersionString;
        }
        
        public async Task<DotNetVersion> Query(CancellationToken cancel)
        {
            var result = await ProcessRunner.RunAndCapture(
                new System.Diagnostics.ProcessStartInfo(NetCommandPathProvider.Path, "--version"),
                cancel: cancel);
            if (result.Errors.Count > 0)
            {
                throw new InvalidOperationException($"{string.Join("\n", result.Errors)}");
            }
            if (result.Out.Count != 1)
            {
                throw new InvalidOperationException($"Unexpected messages:\n{string.Join("\n", result.Out)}");
            }
            return NugetVersionString.Parse(result.Out[0]);
        }
    }
}