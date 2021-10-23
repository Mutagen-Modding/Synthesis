using System;
using System.Windows.Input;
using Noggog.WPF;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.WorkEngine;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class GlobalSettingsVm : ViewModel
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

        [Reactive] public byte BuildCores { get; set; }

        public byte NumProcessors { get; }

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
            BuildCores = settingsSingleton.Gui.BuildCores;
            NumProcessors = (byte)Math.Min(byte.MaxValue, Environment.ProcessorCount);
            
            this.WhenAnyValue(x => x.BuildCores)
                .Subscribe(x => workConsumerSettings.SetNumThreads(x))
                .DisposeWith(this);
            
            saveSignal.Saving
                .Subscribe(x => Save(x.Gui))
                .DisposeWith(this);
        }

        public void SetPrevious(ViewModel? previous)
        {
            _previous = previous;
        }

        private void Save(SynthesisGuiSettings settings)
        {
            settings.BuildCores = BuildCores;
        }
    }
}