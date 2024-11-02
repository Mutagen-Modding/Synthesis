using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing;

public interface IPipelineSettingsPath
{
    FileName Name { get; }
    FilePath Path { get; }
}

[ExcludeFromCodeCoverage]
public class PipelineSettingsPath : IPipelineSettingsPath
{
    public FileName Name => "PipelineSettings.json";
    public FilePath Path => "PipelineSettings.json";
}