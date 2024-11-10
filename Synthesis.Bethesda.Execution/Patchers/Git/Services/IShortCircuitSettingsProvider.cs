namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public interface IShortCircuitSettingsProvider
{
    bool Shortcircuit { get; set; }
}