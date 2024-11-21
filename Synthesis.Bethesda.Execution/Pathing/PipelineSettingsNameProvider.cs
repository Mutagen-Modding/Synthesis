using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing;

public interface IPipelineSettingsNameProvider
{
    FileName Name { get; }
}

public class PipelineSettingsNameProvider : IPipelineSettingsNameProvider
{
    public FileName Name => "PipelineSettings.json";
}