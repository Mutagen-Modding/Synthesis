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
            CancellationToken cancel,
            bool logOutput = true);

        Task<int> Run(
            ProcessStartInfo startInfo,
            Action<string> outputCallback,
            Action<string> errorCallback,
            CancellationToken cancel);
    }

    public class ProcessRunner : IProcessRunner
    {
        public ILogger Logger { get; }
        public IProcessFactory Factory { get; }

        public ProcessRunner(
            ILogger logger,
            IProcessFactory processFactory)
        {
            Logger = logger;
            Factory = processFactory;
        }
        
        public async Task<ProcessRunReturn> RunAndCapture(
            ProcessStartInfo startInfo,
            CancellationToken cancel)
        {
            var outs = new List<string>();
            var errs = new List<string>();
            var ret = await Run(
                startInfo,
                i => outs.Add(i),
                i => errs.Add(i),
                cancel);
            return new(ret, outs, errs);
        }

        private async Task<int> RunNoLog(ProcessStartInfo startInfo, CancellationToken cancel)
        {
            using var proc = Factory.Create(
                startInfo,
                cancel: cancel);
            Logger.Information("({WorkingDirectory}): {FileName} {Args}",
                startInfo.WorkingDirectory,
                startInfo.FileName,
                startInfo.Arguments);
            return await proc.Run();
        }

        public async Task<int> Run(ProcessStartInfo startInfo, CancellationToken cancel, bool logOutput)
        {
            if (logOutput)
            {
                return await Run(startInfo, Logger.Information, Logger.Error, cancel);
            }
            else
            {
                return await RunNoLog(startInfo, cancel);
            }
        }

        public async Task<int> Run(
            ProcessStartInfo startInfo,
            Action<string> outputCallback, 
            Action<string> errorCallback, 
            CancellationToken cancel)
        {
            using var proc = Factory.Create(
                startInfo,
                cancel: cancel);
            Logger.Information("({WorkingDirectory}): {FileName} {Args}",
                startInfo.WorkingDirectory,
                startInfo.FileName,
                startInfo.Arguments);
            using var outSub = proc.Output.Subscribe(outputCallback);
            using var errSub = proc.Error.Subscribe(errorCallback);
            return await proc.Run();
        }
    }
}