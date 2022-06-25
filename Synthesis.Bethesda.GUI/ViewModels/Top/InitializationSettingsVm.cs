using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public class InitializationSettingsVm : ViewModel, IModifySavingSettings
{
    [Reactive]
    public bool ShowUnlisted { get; set; }
    
    [Reactive]
    public bool ShowInstalled { get; set; }

    public InitializationSettingsVm(ISettingsSingleton settingsSingleton)
    {
        ShowUnlisted = settingsSingleton.Gui.BrowserSettings.ShowUnlisted;
        ShowInstalled = settingsSingleton.Gui.BrowserSettings.ShowInstalled;
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        gui.BrowserSettings = new BrowserSettings(
            ShowInstalled: ShowInstalled,
            ShowUnlisted: ShowUnlisted);
    }
}