using System.Windows.Input;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.GUI.Settings;

public interface IShowHelpSetting
{
    bool ShowHelp { get; set; }
    ICommand ShowHelpToggleCommand { get; }
}

public class ShowHelpSetting : ViewModel, IShowHelpSetting, IModifySavingSettings
{
    [Reactive]
    public bool ShowHelp { get; set; }

    public ICommand ShowHelpToggleCommand { get; }
        
    public ShowHelpSetting(
        ISettingsSingleton settings)
    {
        ShowHelpToggleCommand = ReactiveCommand.Create(() => ShowHelp = !ShowHelp);

        ShowHelp = settings.Gui.ShowHelp;
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        gui.ShowHelp = ShowHelp;
    }
}