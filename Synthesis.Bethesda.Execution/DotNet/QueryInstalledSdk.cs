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
        DotNetVersion ParseVersionString(ReadOnlySpan<char> str);
    }

    public class QueryInstalledSdk : IQueryInstalledSdk
    {
        public const int MinVersion = 5;
        
        public async Task<DotNetVersion> Query(CancellationToken cancel)
        {
            using var proc = ProcessWrapper.Create(
                new System.Diagnostics.ProcessStartInfo("dotnet", "--version"),
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
            return ParseVersionString(outs[0]);
        }

        public DotNetVersion ParseVersionString(ReadOnlySpan<char> str)
        {
            var orig = str;
            var indexOf = str.IndexOf('-');
            if (indexOf != -1)
            {
                str = str.Slice(0, indexOf);
            }
            if (Version.TryParse(str, out var vers)
                && vers.Major >= MinVersion)
            {
                return new DotNetVersion(orig.ToString(), true);
            }
            return new DotNetVersion(orig.ToString(), false);
        }
    }
}