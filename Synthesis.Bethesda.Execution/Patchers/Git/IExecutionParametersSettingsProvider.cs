namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IExecutionParametersSettingsProvider
{
    string? TargetRuntime { get; }
}