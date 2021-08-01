using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IPipelineSettingsPath
    {
        string Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class PipelineSettingsPath : IPipelineSettingsPath
    {
        public string Path => "PipelineSettings.json";
    }
}