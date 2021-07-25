using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Noggog.Utility;
using Serilog;

namespace Synthesis.Bethesda.Execution.Utility
{
    public interface IProcessRunner
    {
        Task<ProcessRunReturn> RunAndCapture(
            ProcessStartInfo startInfo,
            CancellationToken cancel);
        Task<int> Run(
            ProcessStartInfo startInfo,
            CancellationToken cancel);
    }

    public class ProcessRunner : IProcessRunner
    {
        private readonly ILogger _logger;
        public IProcessFactory Factory { get; }

        public ProcessRunner(
            ILogger logger,
            IProcessFactory processFactory)
        {
            _logger = logger;
            Factory = processFactory;
        }
        
        public async Task<ProcessRunReturn> RunAndCapture(
            ProcessStartInfo startInfo,
            CancellationToken cancel)
        {
            using var proc = Factory.Create(
                startInfo,
                cancel: cancel);
            _logger.Information("({WorkingDirectory}): {FileName} {Args}",
                startInfo.WorkingDirectory,
                startInfo.FileName,
                startInfo.Arguments);
            var outs = new List<string>();
            using var outp = proc.Output.Subscribe(o => outs.Add(o));
            var errs = new List<string>();
            using var errp = proc.Error.Subscribe(o => errs.Add(o));
            return new(await proc.Run(), outs, errs);
        }

        public async Task<int> Run(ProcessStartInfo startInfo, CancellationToken cancel)
        {
            using var proc = Factory.Create(
                startInfo,
                cancel: cancel);
            _logger.Information("({WorkingDirectory}): {FileName} {Args}",
                startInfo.WorkingDirectory,
                startInfo.FileName,
                startInfo.Arguments);
            return await proc.Run();
        }
    }
}