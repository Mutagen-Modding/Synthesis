using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Settings;
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
    IExecutionParametersSettingsProvider
{
    [Reactive] public bool Shortcircuit { get; set; }

    [Reactive] public string DotNetPathOverride { get; set; }

    private readonly ObservableAsPropertyHelper<byte> _buildCores;
    public byte BuildCores => _buildCores.Value;
        
    [Reactive] public double BuildCorePercentage { get; set; }

    [Reactive] public bool SpecifyTargetFramework { get; set; } = true;

    public string? TargetRuntime => SpecifyTargetFramework ? "win-x64" : null;
    
    public GlobalSettingsVm(
        ISettingsSingleton settingsSingleton,
        BuildCoreCalculator calculator)
    {
        Shortcircuit = settingsSingleton.Pipeline.Shortcircuit;
        DotNetPathOverride = settingsSingleton.Pipeline.DotNetPathOverride;
        BuildCorePercentage = settingsSingleton.Pipeline.BuildCorePercentage;
        SpecifyTargetFramework = settingsSingleton.Gui.SpecifyTargetFramework;

        _buildCores = this.WhenAnyValue(x => x.BuildCorePercentage)
            .Select(calculator.Calculate)
            .ToGuiProperty(this, nameof(BuildCores), deferSubscription: true);
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        pipe.BuildCorePercentage = BuildCorePercentage;
        gui.SpecifyTargetFramework = SpecifyTargetFramework;
        pipe.DotNetPathOverride = DotNetPathOverride;
        pipe.Shortcircuit = Shortcircuit;
    }

    public IObservable<int?> NumDesiredThreads => this.WhenAnyValue(x => x.BuildCores).Select(x => (int?)x);
}