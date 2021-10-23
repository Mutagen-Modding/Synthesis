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

        Task<int> RunWithCallback(
            ProcessStartInfo startInfo,
            Action<string> outputCallback,
            Action<string> errorCallback,
            CancellationToken cancel);

        Task<int> RunWithCallback(
            ProcessStartInfo startInfo,
            Action<string> callback,
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
            var ret = await RunWithCallback(
                startInfo,
                i => outs.Add(i),
                i => errs.Add(i),
                cancel).ConfigureAwait(false);
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
            return await proc.Run().ConfigureAwait(false);
        }

        public async Task<int> Run(ProcessStartInfo startInfo, CancellationToken cancel, bool logOutput)
        {
            if (logOutput)
            {
                return await RunWithCallback(startInfo, Logger.Information, Logger.Error, cancel).ConfigureAwait(false);
            }
            else
            {
                return await RunNoLog(startInfo, cancel).ConfigureAwait(false);
            }
        }

        public async Task<int> RunWithCallback(
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
            return await proc.Run().ConfigureAwait(false);
        }

        public async Task<int> RunWithCallback(
            ProcessStartInfo startInfo,
            Action<string> callback, 
            CancellationToken cancel)
        {
            return await RunWithCallback(
                startInfo,
                callback,
                callback,
                cancel).ConfigureAwait(false);;
        }
    }
}