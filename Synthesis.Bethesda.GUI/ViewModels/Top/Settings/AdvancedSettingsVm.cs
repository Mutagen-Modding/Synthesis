using System;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Settings;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Patchers.Git;
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

    public byte NumProcessors { get; }

    public GlobalSettingsVm(
        IWorkConsumerSettings workConsumerSettings,
        ISettingsSingleton settingsSingleton)
    {
        Shortcircuit = settingsSingleton.Pipeline.Shortcircuit;
        DotNetPathOverride = settingsSingleton.Gui.DotNetPathOverride;
        BuildCorePercentage = settingsSingleton.Gui.BuildCorePercentage;
            
        NumProcessors = (byte)Math.Min(byte.MaxValue, Environment.ProcessorCount);

        _buildCores = this.WhenAnyValue(x => x.BuildCorePercentage)
            .Select(x => (byte)Math.Min(byte.MaxValue, Environment.ProcessorCount * Percent.FactoryPutInRange(x)))
            .ToGuiProperty(this, nameof(BuildCores), deferSubscription: true);
            
        ObservableExtensions.Subscribe(this.WhenAnyValue(x => x.BuildCores), x => workConsumerSettings.SetNumThreads(x))
            .DisposeWith(this);
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        gui.BuildCorePercentage = BuildCorePercentage;
        gui.DotNetPathOverride = DotNetPathOverride;
        pipe.Shortcircuit = Shortcircuit;
    }
}