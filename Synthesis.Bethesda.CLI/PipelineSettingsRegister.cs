using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI;

public class PipelineSettingsRegister : IDotNetPathSettingsProvider
{
    private readonly IPipelineSettings _pipelineSettings;

    public PipelineSettingsRegister(IPipelineSettings pipelineSettings)
    {
        _pipelineSettings = pipelineSettings;
    }

    public string DotNetPathOverride => _pipelineSettings.DotNetPathOverride;
}