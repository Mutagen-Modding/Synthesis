using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IExecutionParameters
{
    string Parameters { get; }
}

[ExcludeFromCodeCoverage]
public class ExecutionParameters : IExecutionParameters
{
    private readonly IExecutionParametersSettingsProvider _parametersSettingsProvider;

    public string Parameters => $"{(_parametersSettingsProvider.SpecifyTargetFramework ? "--runtime win-x64 " : null)}-c Release";

    public ExecutionParameters(IExecutionParametersSettingsProvider parametersSettingsProvider)
    {
        _parametersSettingsProvider = parametersSettingsProvider;
    }
}