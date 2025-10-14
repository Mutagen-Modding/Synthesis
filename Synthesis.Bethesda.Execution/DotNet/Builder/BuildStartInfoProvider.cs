using System.Diagnostics;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.DotNet.Builder;

public interface IBuildStartInfoProvider
{
    ProcessStartInfo Construct(FilePath path);
}

public class BuildStartInfoProvider : IBuildStartInfoProvider
{
    public IExecutionParameters ExecutionParameters { get; }
    public IDotNetCommandStartConstructor StartConstructor { get; }
    private readonly IMo2CompatibilitySettingsProvider _settings;

    public BuildStartInfoProvider(
        IExecutionParameters executionParameters,
        IDotNetCommandStartConstructor startConstructor,
        IMo2CompatibilitySettingsProvider settings)
    {
        ExecutionParameters = executionParameters;
        StartConstructor = startConstructor;
        _settings = settings;
    }

    public ProcessStartInfo Construct(FilePath path)
    {
        var buildCommand = _settings.Mo2Compatibility
            ? "build -maxcpucount:1 /p:BuildInParallel=false --disable-build-servers"
            : "build";
        return StartConstructor.Construct(buildCommand, path, ExecutionParameters.Parameters);
    }
}