using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IQueryInstalledSdk
    {
        Task<DotNetVersion> Query(CancellationToken cancel);
    }

    public class QueryInstalledSdk : IQueryInstalledSdk
    {
        private readonly IDotNetCommandPathProvider _dotNetCommandPathProvider;
        private readonly IParseNugetVersionString _parseNugetVersionString;
        private readonly IProcessFactory _ProcessFactory;

        public QueryInstalledSdk(
            IDotNetCommandPathProvider dotNetCommandPathProvider,
            IParseNugetVersionString parseNugetVersionString,
            IProcessFactory processFactory)
        {
            _dotNetCommandPathProvider = dotNetCommandPathProvider;
            _parseNugetVersionString = parseNugetVersionString;
            _ProcessFactory = processFactory;
        }
        
        public async Task<DotNetVersion> Query(CancellationToken cancel)
        {
            using var proc = _ProcessFactory.Create(
                new System.Diagnostics.ProcessStartInfo(_dotNetCommandPathProvider.Path, "--version"),
                cancel: cancel);
            List<string> outs = new();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            List<string> errs = new();
            using var errp = proc.Error.Subscribe(o => errs.Add(o));
            var result = await proc.Run();
            if (errs.Count > 0)
            {
                throw new ArgumentException($"{string.Join("\n", errs)}");
            }
            if (outs.Count != 1)
            {
                throw new ArgumentException($"Unexpected messages:\n{string.Join("\n", outs)}");
            }
            return _parseNugetVersionString.Parse(outs[0]);
        }
    }
}