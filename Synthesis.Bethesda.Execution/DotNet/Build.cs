using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Noggog.Utility;
using Serilog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IBuild
    {
        Task<ErrorResponse> Compile(string targetPath, CancellationToken cancel);
    }

    public class Build : IBuild
    {
        private readonly ILogger _Logger;
        private readonly IProcessFactory _processFactory;
        private readonly IBuildStartProvider _buildStartProvider;

        public Build(
            ILogger logger,
            IProcessFactory processFactory,
            IBuildStartProvider buildStartProvider)
        {
            _Logger = logger;
            _processFactory = processFactory;
            _buildStartProvider = buildStartProvider;
        }
        
        public async Task<ErrorResponse> Compile(string targetPath, CancellationToken cancel)
        {
            var start = _buildStartProvider.Construct(Path.GetFileName(targetPath));
            start.WorkingDirectory = Path.GetDirectoryName(targetPath)!;
            using var process = _processFactory.Create(
                start,
                cancel: cancel);
            _Logger.Information("({WorkingDirectory}): {FileName} {Args}",
                process.StartInfo.WorkingDirectory,
                process.StartInfo.FileName,
                process.StartInfo.Arguments);
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