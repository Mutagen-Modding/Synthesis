using Serilog;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet;

public class Mo2MsBuildServerDisabler
{
    private const string DoNotUseServerVar = "DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER";
    private const string UseServerVar = "MSBUILDUSESERVER";

    private readonly ILogger _logger;
    private readonly IMo2EnvironmentDetector _mo2Detector;
    private readonly string? _originalDoNotUseServer;
    private readonly string? _originalUseServer;

    public Mo2MsBuildServerDisabler(
        ILogger logger,
        IMo2EnvironmentDetector mo2Detector)
    {
        _logger = logger;
        _mo2Detector = mo2Detector;
        _originalDoNotUseServer = Environment.GetEnvironmentVariable(DoNotUseServerVar);
        _originalUseServer = Environment.GetEnvironmentVariable(UseServerVar);
    }

    // MSBuild server fails to start under USVFS, and the fallback warning on stderr is treated as a failure
    public void Apply(bool mo2ModeEnabled)
    {
        if (mo2ModeEnabled && _mo2Detector.IsRunningInsideMo2())
        {
            _logger.Information("MO2 mode: disabling MSBuild server for child processes");
            Environment.SetEnvironmentVariable(DoNotUseServerVar, "1");
            Environment.SetEnvironmentVariable(UseServerVar, "0");
        }
        else
        {
            Environment.SetEnvironmentVariable(DoNotUseServerVar, _originalDoNotUseServer);
            Environment.SetEnvironmentVariable(UseServerVar, _originalUseServer);
        }
    }
}
