using System.Windows.Input;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for CscCrashErrorClassification.
/// Exposes MO2 detection status and protection settings for the view.
/// </summary>
public class CscCrashErrorVm : ErrorClassificationVm
{
    private readonly CscCrashErrorClassification _error;

    public string CompilationText => _error.CompilationText;
    public bool IsRunningInsideMo2 => _error.IsRunningInsideMo2;

    /// <summary>
    /// Global settings VM for accessing BlockBuildingWithinMo2 setting
    /// </summary>
    public GlobalSettingsVm GlobalSettings { get; }

    /// <summary>
    /// Whether the "Enable Protection" section should be visible.
    /// Only shown if running inside MO2 and protection is not already on.
    /// </summary>
    public bool ShowEnableProtection { get; }

    /// <summary>
    /// Command to open the antivirus FAQ discussion in a browser
    /// </summary>
    public ICommand AntivirusReadMoreCommand { get; }

    public delegate CscCrashErrorVm Factory(CscCrashErrorClassification error);

    public CscCrashErrorVm(
        CscCrashErrorClassification error,
        GlobalSettingsVm globalSettings,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;
        GlobalSettings = globalSettings;
        ShowEnableProtection = error.IsRunningInsideMo2 && !globalSettings.BlockBuildingWithinMo2;
        AntivirusReadMoreCommand = ReactiveCommand.Create(
            () => navigateTo.Navigate(CscCrashErrorClassification.FaqLink));
    }
}
