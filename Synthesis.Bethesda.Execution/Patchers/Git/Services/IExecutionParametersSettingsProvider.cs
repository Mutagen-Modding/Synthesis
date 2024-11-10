namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public interface IExecutionParametersSettingsProvider
{
    string? TargetRuntime { get; }
}