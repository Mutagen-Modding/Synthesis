using System;
using System.Windows.Input;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.Settings
{
    public interface IShowHelpSetting
    {
        bool ShowHelp { get; set; }
        ICommand ShowHelpToggleCommand { get; }
    }

    public class ShowHelpSetting : ViewModel, IShowHelpSetting
    {
        public ISaveSignal SaveSignal { get; }

        [Reactive]
        public bool ShowHelp { get; set; }

        public ICommand ShowHelpToggleCommand { get; }
        
        public ShowHelpSetting(
            ISaveSignal saveSignal,
            ISettingsSingleton settings)
        {
            SaveSignal = saveSignal;
            ShowHelpToggleCommand = ReactiveCommand.Create(() => ShowHelp = !ShowHelp);

            ShowHelp = settings.Gui.ShowHelp;
            saveSignal.Saving
                .Subscribe(x => x.Gui.ShowHelp = ShowHelp)
                .DisposeWith(this);
        }
    }
}