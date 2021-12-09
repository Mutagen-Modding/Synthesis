using System;
using System.Reactive.Linq;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Settings;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.WorkEngine;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class GlobalSettingsVm : ViewModel, IShortCircuitSettingsProvider, IDotNetPathSettingsProvider
    {
        [Reactive] public bool Shortcircuit { get; set; }

        [Reactive] public string DotNetPathOverride { get; set; }

        private readonly ObservableAsPropertyHelper<byte> _buildCores;
        public byte BuildCores => _buildCores.Value;
        
        [Reactive] public double BuildCorePercentage { get; set; }

        public byte NumProcessors { get; }

        public GlobalSettingsVm(
            ISaveSignal saveSignal,
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
            
            saveSignal.Saving
                .Subscribe(x =>
                {
                    Save(x.Gui);
                    Save(x.Pipe);
                })
                .DisposeWith(this);
        }

        private void Save(SynthesisGuiSettings settings)
        {
            settings.BuildCorePercentage = BuildCorePercentage;
            settings.DotNetPathOverride = DotNetPathOverride;
        }

        private void Save(IPipelineSettings settings)
        {
            settings.Shortcircuit = Shortcircuit;
        }
    }
}