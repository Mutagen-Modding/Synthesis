using System.ComponentModel;
using Serilog;

namespace Synthesis.Bethesda.Execution.Utility;

public interface IMo2RetryCoordinator
{
    Task<T> OnTransientSpawnFailure<T>(Func<Task<T>> spawn, CancellationToken cancel);
}

public class Mo2RetryCoordinator : IMo2RetryCoordinator
{
    private const int MaxAttempts = 5;
    private const int BaseBackoffMs = 250;
    private const int MaxBackoffMs = 2000;
    private const int ErrorAccessDenied = 5;

    private readonly ILogger _logger;
    private readonly IMo2EnvironmentDetector _mo2Detector;

    public Mo2RetryCoordinator(
        ILogger logger,
        IMo2EnvironmentDetector mo2Detector)
    {
        _logger = logger;
        _mo2Detector = mo2Detector;
    }

    /// <summary>
    /// Retries a child-process spawn that fails at pipe/process creation with a transient
    /// Win32 "Access is denied". This only occurs under MO2's usvfs, so outside MO2 the spawn
    /// runs once with no retry. The final attempt's exception propagates.
    /// </summary>
    public async Task<T> OnTransientSpawnFailure<T>(Func<Task<T>> spawn, CancellationToken cancel)
    {
        if (!_mo2Detector.IsRunningInsideMo2())
        {
            return await spawn().ConfigureAwait(false);
        }

        for (int attempt = 1; ; attempt++)
        {
            cancel.ThrowIfCancellationRequested();
            try
            {
                return await spawn().ConfigureAwait(false);
            }
            catch (Win32Exception ex) when (attempt < MaxAttempts && ex.NativeErrorCode == ErrorAccessDenied)
            {
                var backoff = Math.Min(MaxBackoffMs, BaseBackoffMs << (attempt - 1));
                _logger.Warning(ex,
                    "Process spawn failed at start inside MO2 (attempt {Attempt}/{MaxAttempts}); retrying in {BackoffMs}ms",
                    attempt, MaxAttempts, backoff);
                await Task.Delay(backoff, cancel).ConfigureAwait(false);
            }
        }
    }
}
