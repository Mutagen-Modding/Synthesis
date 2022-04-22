using System.Reactive.Linq;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Settings;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings.Calculators;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

public class GlobalSettingsVm : ViewModel, IShortCircuitSettingsProvider, IDotNetPathSettingsProvider, IModifySavingSettings
{
    [Reactive] public bool Shortcircuit { get; set; }

    [Reactive] public string DotNetPathOverride { get; set; }

    private readonly ObservableAsPropertyHelper<byte> _buildCores;
    public byte BuildCores => _buildCores.Value;
        
    [Reactive] public double BuildCorePercentage { get; set; }

    public GlobalSettingsVm(
        IWorkConsumerSettings workConsumerSettings,
        ISettingsSingleton settingsSingleton,
        BuildCoreCalculator calculator)
    {
        Shortcircuit = settingsSingleton.Pipeline.Shortcircuit;
        DotNetPathOverride = settingsSingleton.Pipeline.DotNetPathOverride;
        BuildCorePercentage = settingsSingleton.Pipeline.BuildCorePercentage;

        _buildCores = this.WhenAnyValue(x => x.BuildCorePercentage)
            .Select(calculator.Calculate)
            .ToGuiProperty(this, nameof(BuildCores), deferSubscription: true);
            
        ObservableExtensions.Subscribe(this.WhenAnyValue(x => x.BuildCores), x => workConsumerSettings.SetNumThreads(x))
            .DisposeWith(this);
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        pipe.BuildCorePercentage = BuildCorePercentage;
        pipe.DotNetPathOverride = DotNetPathOverride;
        pipe.Shortcircuit = Shortcircuit;
    }
}