namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IExecutionParametersSettingsProvider
{
    bool SpecifyTargetFramework { get; set; }
}