using System.Diagnostics;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IProjectRunProcessStartInfoProvider
{
    ProcessStartInfo GetStart(string path, string args);
}

public class ProjectRunProcessStartInfoProvider : IProjectRunProcessStartInfoProvider
{
    public IExecutionParameters ExecutionParameters { get; }
    public IDotNetCommandStartConstructor CmdStartConstructor { get; }

    public ProjectRunProcessStartInfoProvider(
        IExecutionParameters executionParameters,
        IDotNetCommandStartConstructor cmdStartConstructor)
    {
        ExecutionParameters = executionParameters;
        CmdStartConstructor = cmdStartConstructor;
    }

    public ProcessStartInfo GetStart(string path, string args)
    {
        return CmdStartConstructor.Construct("run --project", path,
            ExecutionParameters.Parameters,
            "--no-build",
            args);
    }
}