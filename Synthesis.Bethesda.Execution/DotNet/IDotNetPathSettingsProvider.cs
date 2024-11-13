namespace Synthesis.Bethesda.Execution.DotNet;

public interface IDotNetPathSettingsProvider
{
    string DotNetPathOverride { get; }
}

public class DotNetPathSettingsInjection : IDotNetPathSettingsProvider
{
    public string DotNetPathOverride => string.Empty;
}