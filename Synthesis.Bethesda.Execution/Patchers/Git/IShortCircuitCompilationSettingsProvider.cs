namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IShortCircuitCompilationSettingsProvider
    {
        bool ShortcircuitBuilds { get; set; }
    }
}