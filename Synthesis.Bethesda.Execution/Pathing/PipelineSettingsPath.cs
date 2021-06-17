namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IPipelineSettingsPath
    {
        string Path { get; }
    }

    public class PipelineSettingsPath : IPipelineSettingsPath
    {
        public string Path => "PipelineSettings.json";
    }
}