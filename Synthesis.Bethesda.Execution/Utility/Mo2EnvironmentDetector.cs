using System.Diagnostics;
using Serilog;

namespace Synthesis.Bethesda.Execution.Utility;

/// <summary>
/// Detects if the application is running within Mod Organizer 2 by checking for USVFS DLLs
/// </summary>
public class Mo2EnvironmentDetector : IMo2EnvironmentDetector
{
    private readonly ILogger _logger;
    private readonly Lazy<bool> _isMo2Detected;

    private static readonly string[] UsvfsDllNames =
    {
        "usvfs_x64.dll",
        "usvfs_x86.dll"
    };

    public Mo2EnvironmentDetector(ILogger logger)
    {
        _logger = logger;
        _isMo2Detected = new Lazy<bool>(DetectMo2);
    }

    /// <inheritdoc />
    public bool IsRunningInsideMo2()
    {
        return _isMo2Detected.Value;
    }

    private bool DetectMo2()
    {
        // Check for artificial marker first (for testing/debugging)
        var marker = Environment.GetEnvironmentVariable("SYNTHESIS_MO2_MARKER_FOR_TESTING");
        if (!string.IsNullOrEmpty(marker))
        {
            _logger.Information("MO2 detected: SYNTHESIS_MO2_MARKER environment variable is set");
            return true;
        }

        try
        {
            var currentProcess = Process.GetCurrentProcess();
            foreach (ProcessModule module in currentProcess.Modules)
            {
                var moduleName = module.ModuleName;
                foreach (var dllName in UsvfsDllNames)
                {
                    if (string.Equals(moduleName, dllName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Information("MO2 detected: {DllName} is loaded", dllName);
                        return true;
                    }
                }
            }

            _logger.Information("MO2 not detected: No USVFS DLLs found");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error detecting MO2 environment, assuming not running in MO2");
            return false;
        }
    }
}
