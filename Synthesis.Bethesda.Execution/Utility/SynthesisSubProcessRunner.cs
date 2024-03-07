using System.Diagnostics;
using System.Runtime.InteropServices;
using Noggog.Processes.DI;
using Serilog;

namespace Synthesis.Bethesda.Execution.Utility;

public interface ISynthesisSubProcessRunner
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

public class SynthesisSubProcessRunner : ISynthesisSubProcessRunner
{
    public ILogger Logger { get; }
    public IProcessFactory Factory { get; }

    private readonly bool _killWithParent;

    public SynthesisSubProcessRunner(
        ILogger logger,
        IProcessFactory processFactory)
    {
        Logger = logger;
        Factory = processFactory;
        _killWithParent = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
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
            cancel: cancel,
            killWithParent: _killWithParent);
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
            cancel: cancel,
            killWithParent: _killWithParent);
        Logger.Information("({WorkingDirectory}): {FileName} {Args}",
            startInfo.WorkingDirectory,
            startInfo.FileName,
            startInfo.Arguments);
        using var outSub = proc.Output.Subscribe(outputCallback);
        List<string> errs = new();
        using var errSub = proc.Error.Subscribe(errs.Add);
        try
        {
            return await proc.Run().ConfigureAwait(false);
        }
        finally
        {
            foreach (var err in errs)
            {
                errorCallback(err);
            }
        }
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