namespace Synthesis.Bethesda.Execution.DotNet;

public interface IDotNetPathSettingsProvider
{
    string DotNetPathOverride { get; }
}

public class DotNetPathSettingsProvider : IDotNetPathSettingsProvider
{
    public string DotNetPathOverride => string.Empty;
}