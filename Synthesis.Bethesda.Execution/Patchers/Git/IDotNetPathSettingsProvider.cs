namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IDotNetPathSettingsProvider
    {
        string DotNetPathOverride { get; }
    }

    public class DotNetPathSettingsProvider : IDotNetPathSettingsProvider
    {
        public string DotNetPathOverride => string.Empty;
    }
}