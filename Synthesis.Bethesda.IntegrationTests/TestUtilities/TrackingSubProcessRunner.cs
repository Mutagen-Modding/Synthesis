using System.Collections.Concurrent;
using System.Diagnostics;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.IntegrationTests.TestUtilities;

/// <summary>
/// Test wrapper for ISynthesisSubProcessRunner that tracks all ProcessStartInfo executions.
/// NOTE: This is in TestUtilities namespace (not Components) to avoid auto-registration.
/// It should only be registered explicitly in specific tests.
/// </summary>
public class TrackingSubProcessRunner : ISynthesisSubProcessRunner
{
    private readonly ISynthesisSubProcessRunner _inner;

    /// <summary>
    /// All ProcessStartInfo objects that have been executed
    /// </summary>
    public ConcurrentBag<ProcessStartInfo> ExecutedProcesses { get; } = new();

    public TrackingSubProcessRunner(ISynthesisSubProcessRunner inner)
    {
        _inner = inner;
    }

    private void Track(ProcessStartInfo startInfo)
    {
        ExecutedProcesses.Add(startInfo);
    }

    public async Task<ProcessRunReturn> RunAndCapture(ProcessStartInfo startInfo, CancellationToken cancel)
    {
        Track(startInfo);
        return await _inner.RunAndCapture(startInfo, cancel);
    }

    public async Task<int> Run(ProcessStartInfo startInfo, CancellationToken cancel, bool logOutput = true)
    {
        Track(startInfo);
        return await _inner.Run(startInfo, cancel, logOutput);
    }

    public async Task<int> RunWithCallback(ProcessStartInfo startInfo, Action<string> outputCallback, Action<string> errorCallback, CancellationToken cancel)
    {
        Track(startInfo);
        return await _inner.RunWithCallback(startInfo, outputCallback, errorCallback, cancel);
    }

    public async Task<int> RunWithCallback(ProcessStartInfo startInfo, Action<string> callback, CancellationToken cancel)
    {
        Track(startInfo);
        return await _inner.RunWithCallback(startInfo, callback, cancel);
    }

    public async Task<ProcessRunReturn> RunAndCaptureWithLogging(ProcessStartInfo startInfo, CancellationToken cancel)
    {
        Track(startInfo);
        return await _inner.RunAndCaptureWithLogging(startInfo, cancel);
    }

    public async Task<int> RunWithCapture(ProcessStartInfo startInfo, PatcherRunCapture capture, CancellationToken cancel)
    {
        Track(startInfo);
        return await _inner.RunWithCapture(startInfo, capture, cancel);
    }
}
