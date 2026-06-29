using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.IntegrationTests.Components;

/// <summary>
/// Execution parameters for tests that disable the shared Roslyn compilation server
/// to prevent file locking issues between test runs on CI.
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
