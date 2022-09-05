using System.Reactive.Linq;
using Synthesis.Bethesda.Execution.Settings.Calculators;
using Noggog.WorkEngine;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class SetWorkerThreads : INumWorkThreadsController
{
    private readonly IPipelineSettingsProvider _pipelineSettings;
    private readonly BuildCoreCalculator _coreCalculator;

    public SetWorkerThreads(
        IPipelineSettingsProvider pipelineSettings,
        BuildCoreCalculator coreCalculator)
    {
        _pipelineSettings = pipelineSettings;
        _coreCalculator = coreCalculator;
    }

    public IObservable<int?> NumDesiredThreads => Observable
        .Return(_coreCalculator.Calculate(_pipelineSettings.Settings.BuildCorePercentage)).Select(x => (int?)x);
}