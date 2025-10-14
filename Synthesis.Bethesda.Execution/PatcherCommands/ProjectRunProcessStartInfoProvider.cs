using System.Diagnostics;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IProjectRunProcessStartInfoProvider
{
    ProcessStartInfo GetStart(string path, string args, bool build = false);
}

public class ProjectRunProcessStartInfoProvider : IProjectRunProcessStartInfoProvider
{
    public IExecutionParameters ExecutionParameters { get; }
    public IDotNetCommandStartConstructor CmdStartConstructor { get; }
    private readonly IMo2CompatibilitySettingsProvider _settings;

    public ProjectRunProcessStartInfoProvider(
        IExecutionParameters executionParameters,
        IDotNetCommandStartConstructor cmdStartConstructor,
        IMo2CompatibilitySettingsProvider settings)
    {
        ExecutionParameters = executionParameters;
        CmdStartConstructor = cmdStartConstructor;
        _settings = settings;
    }

    public ProcessStartInfo GetStart(string path, string args, bool build = false)
    {
        string buildParam;
        if (!build)
        {
            buildParam = "--no-build";
        }
        else if (_settings.Mo2Compatibility)
        {
            buildParam = "-maxcpucount:1 /p:BuildInParallel=false --disable-build-servers";
        }
        else
        {
            buildParam = string.Empty;
        }

        return CmdStartConstructor.Construct("run --project", path,
            ExecutionParameters.Parameters,
            buildParam,
            args);
    }
}