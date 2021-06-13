using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution
{
    public interface IBuild
    {
        Task<ErrorResponse> Compile(string targetPath, CancellationToken cancel, Action<string>? log);
    }

    public class Build : IBuild
    {
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideBuildString _BuildString;

        public Build(
            IProcessFactory processFactory,
            IProvideBuildString buildString)
        {
            _ProcessFactory = processFactory;
            _BuildString = buildString;
        }
        
        public async Task<ErrorResponse> Compile(string targetPath, CancellationToken cancel, Action<string>? log)
        {
            using var process = _ProcessFactory.Create(
                new ProcessStartInfo("dotnet", _BuildString.Get($"\"{Path.GetFileName(targetPath)}\""))
                {
                    WorkingDirectory = Path.GetDirectoryName(targetPath)!
                },
                cancel: cancel);
            log?.Invoke($"({process.StartInfo.WorkingDirectory}): {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            string? firstError = null;
            bool buildFailed = false;
            List<string> output = new();
            int totalLen = 0;
            process.Output.Subscribe(o =>
            {
                // ToDo
                // Refactor off looking for a string
                if (o.StartsWith("Build FAILED"))
                {
                    buildFailed = true;
                }
                else if (buildFailed
                         && firstError == null
                         && !string.IsNullOrWhiteSpace(o)
                         && o.StartsWith("error"))
                {
                    firstError = o;
                }
                if (totalLen < 10_000)
                {
                    totalLen += o.Length;
                    output.Add(o);
                }
            });
            var result = await process.Run().ConfigureAwait(false);
            if (result == 0) return ErrorResponse.Success;
            firstError = firstError?.TrimStart($"{targetPath} : ");
            if (firstError == null && cancel.IsCancellationRequested)
            {
                firstError = "Cancelled";
            }
            return ErrorResponse.Fail(reason: firstError ?? $"Unknown Error: {string.Join(Environment.NewLine, output)}");
        }
    }
}