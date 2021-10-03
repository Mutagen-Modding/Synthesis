using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IPipelineSettingsPath
    {
        FilePath Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class PipelineSettingsPath : IPipelineSettingsPath
    {
        public FilePath Path => "PipelineSettings.json";
    }
}