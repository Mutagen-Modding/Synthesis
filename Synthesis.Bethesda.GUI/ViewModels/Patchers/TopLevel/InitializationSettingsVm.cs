using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

public class InitializationSettingsVm : ViewModel, IModifySavingSettings
{
    [Reactive]
    public bool ShowAllGitPatchersInBrowser { get; set; }

    public InitializationSettingsVm(ISettingsSingleton settingsSingleton)
    {
        ShowAllGitPatchersInBrowser = settingsSingleton.Gui.ShowAllGitPatchersInBrowser;
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        gui.ShowAllGitPatchersInBrowser = ShowAllGitPatchersInBrowser;
    }
}