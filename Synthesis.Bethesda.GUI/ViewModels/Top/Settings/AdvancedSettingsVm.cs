using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Settings;
using Noggog.Reactive;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Settings.Calculators;
using Synthesis.Bethesda.Execution.Settings.V2;
using Noggog.WorkEngine;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

public class GlobalSettingsVm : ViewModel,
    IShortCircuitSettingsProvider, IDotNetPathSettingsProvider,
    IModifySavingSettings, INumWorkThreadsController,
    IExecutionParametersSettingsProvider,
    IBlockBuildingWithinMo2SettingsProvider
{
    [Reactive] public bool Shortcircuit { get; set; }

    [Reactive] public bool BlockBuildingWithinMo2 { get; set; }

    private readonly ObservableAsPropertyHelper<bool> _isShortcircuitEditable;
    public bool IsShortcircuitEditable => _isShortcircuitEditable.Value;

    [Reactive] public string DotNetPathOverride { get; set; }

    private readonly ObservableAsPropertyHelper<byte> _buildCores;
    public byte BuildCores => _buildCores.Value;

    [Reactive] public double BuildCorePercentage { get; set; }

    [Reactive] public bool SpecifyTargetFramework { get; set; } = true;

    public string? TargetRuntime => SpecifyTargetFramework ? "win-x64" : null;

    public GlobalSettingsVm(
        ISettingsSingleton settingsSingleton,
        BuildCoreCalculator calculator,
        ISchedulerProvider schedulerProvider)
    {
        Shortcircuit = settingsSingleton.Pipeline.Shortcircuit;
        BlockBuildingWithinMo2 = settingsSingleton.Pipeline.BlockBuildingWithinMo2;
        DotNetPathOverride = settingsSingleton.Pipeline.DotNetPathOverride;
        BuildCorePercentage = settingsSingleton.Pipeline.BuildCorePercentage;
        SpecifyTargetFramework = settingsSingleton.Gui.SpecifyTargetFramework;

        _buildCores = this.WhenAnyValue(x => x.BuildCorePercentage)
            .Select(x => calculator.Calculate(x))
            .ToGuiProperty(this, nameof(BuildCores), scheduler: schedulerProvider.MainThread, deferSubscription: true);

        // When BlockBuildingWithinMo2 is enabled, force Shortcircuit on
        this.WhenAnyValue(x => x.BlockBuildingWithinMo2)
            .Where(x => x)
            .Subscribe(_ => Shortcircuit = true);

        _isShortcircuitEditable = this.WhenAnyValue(x => x.BlockBuildingWithinMo2)
            .Select(x => !x)
            .ToGuiProperty(this, nameof(IsShortcircuitEditable), initialValue: !BlockBuildingWithinMo2, scheduler: schedulerProvider.MainThread);
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        pipe.BuildCorePercentage = BuildCorePercentage;
        gui.SpecifyTargetFramework = SpecifyTargetFramework;
        pipe.DotNetPathOverride = DotNetPathOverride;
        pipe.Shortcircuit = Shortcircuit;
        pipe.BlockBuildingWithinMo2 = BlockBuildingWithinMo2;
    }

    public IObservable<int?> NumDesiredThreads => this.WhenAnyValue(x => x.BuildCores).Select(x => (int?)x);
}