using System.Reactive;
using System.Windows.Input;
using Noggog;
using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public class Mo2PromptVm : ViewModel, IModifySavingSettings
{
    private readonly GlobalSettingsVm _globalSettings;
    private readonly IMo2EnvironmentDetector _mo2Detector;

    public string FaqPage { get; } = "https://github.com/Mutagen-Modding/Synthesis/discussions/562";

    private bool _hasSeenMo2Prompt;

    public bool HasSeenMo2Prompt => _hasSeenMo2Prompt;

    public bool ShouldShow => !_hasSeenMo2Prompt && _mo2Detector.IsRunningInsideMo2();

    [Reactive]
    public bool EnableMo2Mode { get; set; } = true;

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    public ICommand GoToFaqCommand { get; }

    public Mo2PromptVm(
        ISettingsSingleton settingsSingleton,
        GlobalSettingsVm globalSettings,
        IMo2EnvironmentDetector mo2Detector,
        INavigateTo navigateTo)
    {
        _globalSettings = globalSettings;
        _mo2Detector = mo2Detector;
        _hasSeenMo2Prompt = settingsSingleton.Gui.HasSeenMo2Prompt;
        EnableMo2Mode = globalSettings.BlockBuildingWithinMo2 || !_hasSeenMo2Prompt;

        ConfirmCommand = ReactiveCommand.Create(() =>
        {
            _globalSettings.BlockBuildingWithinMo2 = EnableMo2Mode;
            _hasSeenMo2Prompt = true;
        });

        GoToFaqCommand = ReactiveCommand.Create(() => navigateTo.Navigate(FaqPage));
    }

    public void Save(SynthesisGuiSettings gui, PipelineSettings pipe)
    {
        gui.HasSeenMo2Prompt = _hasSeenMo2Prompt;
    }
}
