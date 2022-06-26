using Synthesis.Bethesda.Execution.Settings.Calculators;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class SetWorkerThreads : IStartupTask
{
    private readonly IPipelineSettingsProvider _pipelineSettings;
    private readonly IWorkConsumerSettings _workConsumerSettings;
    private readonly BuildCoreCalculator _coreCalculator;

    public SetWorkerThreads(
        IPipelineSettingsProvider pipelineSettings,
        IWorkConsumerSettings workConsumerSettings,
        BuildCoreCalculator coreCalculator)
    {
        _pipelineSettings = pipelineSettings;
        _workConsumerSettings = workConsumerSettings;
        _coreCalculator = coreCalculator;
    }
    
    public void Start()
    {
        _workConsumerSettings.SetNumThreads(_coreCalculator.Calculate(_pipelineSettings.Settings.BuildCorePercentage));
    }
}