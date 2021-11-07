namespace Synthesis.Bethesda.Execution.Patchers.Running.Git
{
    public interface IShortCircuitCompilationSettingsProvider
    {
        bool ShortcircuitBuilds { get; set; }
    }
}