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

    // Transient Win32 codes MO2's usvfs can surface when a child-process spawn collides
    // with the virtual file system. Retried only inside MO2; a persistent failure still
    // propagates after the final attempt.
    private static readonly HashSet<int> RetriableErrorCodes = new()
    {
        5,    // ERROR_ACCESS_DENIED
        32,   // ERROR_SHARING_VIOLATION
        33,   // ERROR_LOCK_VIOLATION
        1920, // ERROR_CANT_ACCESS_FILE
    };

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
            catch (Win32Exception ex) when (attempt < MaxAttempts && RetriableErrorCodes.Contains(ex.NativeErrorCode))
            {
                var backoff = Math.Min(MaxBackoffMs, BaseBackoffMs << (attempt - 1));
                _logger.Warning(ex,
                    "Process spawn failed at start inside MO2 with Win32 code {ErrorCode} (attempt {Attempt}/{MaxAttempts}); retrying in {BackoffMs}ms",
                    ex.NativeErrorCode, attempt, MaxAttempts, backoff);
                await Task.Delay(backoff, cancel).ConfigureAwait(false);
            }
        }
    }
}
