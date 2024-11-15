using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IExecutionParameters
{
    string Parameters { get; }
}

[ExcludeFromCodeCoverage]
public class ExecutionParameters : IExecutionParameters
{
    private readonly IExecutionParametersSettingsProvider _parametersSettingsProvider;

    public string Parameters => $"{(_parametersSettingsProvider.TargetRuntime == null ? null : $"--runtime {_parametersSettingsProvider.TargetRuntime} ")}-c Release";

    public ExecutionParameters(IExecutionParametersSettingsProvider parametersSettingsProvider)
    {
        _parametersSettingsProvider = parametersSettingsProvider;
    }
}