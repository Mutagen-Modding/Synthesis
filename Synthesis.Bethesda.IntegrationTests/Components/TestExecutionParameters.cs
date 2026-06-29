using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.IntegrationTests.Components;

/// <summary>
/// Execution parameters for tests that disable the shared Roslyn compilation server
/// and MSBuild node reuse to prevent file locking issues between test runs on CI.
/// Without /nodeReuse:false a reused MSBuild worker node can keep a handle on the
/// patcher's deps.json after a build finishes, so a subsequent build of the same
/// project fails in the GenerateDepsFile task with "being used by another process".
/// </summary>
public class TestExecutionParameters : IExecutionParameters
{
    private readonly IExecutionParametersSettingsProvider _parametersSettingsProvider;

    public string Parameters => $"{(_parametersSettingsProvider.TargetRuntime == null ? null : $"--runtime {_parametersSettingsProvider.TargetRuntime} ")}-c Release /p:UseSharedCompilation=false";

    public TestExecutionParameters(IExecutionParametersSettingsProvider parametersSettingsProvider)
    {
        _parametersSettingsProvider = parametersSettingsProvider;
    }
}
