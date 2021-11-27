using System;
using System.Reactive.Linq;
using System.Windows.Input;
using Noggog.WPF;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.WorkEngine;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class GlobalSettingsVm : ViewModel, IShortCircuitCompilationSettingsProvider
    {
        public enum SettingsPages
        {
            General,
            Profile
        };

        public ICommand GoBackCommand { get; }
        
        [Reactive] public SettingsPages SelectedSettings { get; set; }

        private ViewModel? _previous;

        public ProfilesDisplayVm Profiles { get; }

        private readonly ObservableAsPropertyHelper<byte> _buildCores;
        public byte BuildCores => _buildCores.Value;
        
        [Reactive] public double BuildCorePercentage { get; set; }

        public byte NumProcessors { get; }

        [Reactive] public bool ShortcircuitBuilds { get; set; }

        public GlobalSettingsVm(
            ProfilesDisplayVm profilesDisplayVm,
            ISettingsSingleton settingsSingleton,
            ISaveSignal saveSignal,
            IWorkConsumerSettings workConsumerSettings,
            IActivePanelControllerVm activePanelController)
        {
            GoBackCommand = ReactiveCommand.Create(() =>
            {
                activePanelController.ActivePanel = _previous;
            });
            Profiles = profilesDisplayVm;
            BuildCorePercentage = settingsSingleton.Gui.BuildCorePercentage;
            NumProcessors = (byte)Math.Min(byte.MaxValue, Environment.ProcessorCount);
            ShortcircuitBuilds = settingsSingleton.Pipeline.ShortcircuitBuilds;

            _buildCores = this.WhenAnyValue(x => x.BuildCorePercentage)
                .Select(x => (byte)Math.Min(byte.MaxValue, Environment.ProcessorCount * Percent.FactoryPutInRange(x)))
                .ToGuiProperty(this, nameof(BuildCores));
            
            this.WhenAnyValue(x => x.BuildCores)
                .Subscribe(x => workConsumerSettings.SetNumThreads(x))
                .DisposeWith(this);
            
            saveSignal.Saving
                .Subscribe(x =>
                {
                    Save(x.Gui);
                    Save(x.Pipe);
                })
                .DisposeWith(this);
        }

        public void SetPrevious(ViewModel? previous)
        {
            _previous = previous;
        }

        private void Save(SynthesisGuiSettings settings)
        {
            settings.BuildCorePercentage = BuildCorePercentage;
        }

        private void Save(IPipelineSettings settings)
        {
            settings.ShortcircuitBuilds = ShortcircuitBuilds;
        }
    }
}