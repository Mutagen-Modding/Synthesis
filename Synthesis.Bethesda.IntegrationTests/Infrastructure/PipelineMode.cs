namespace Synthesis.Bethesda.IntegrationTests.Infrastructure;

/// <summary>
/// Specifies which pipeline execution mode to use for integration tests
/// </summary>
public enum PipelineMode
{
    /// <summary>
    /// UI-based execution using Synthesis.Bethesda.GUI.Modules.MainModule
    /// </summary>
    UI,

    /// <summary>
    /// CLI-based execution using Synthesis.Bethesda.CLI.RunPipelineCliModule
    /// </summary>
    CLI
}
