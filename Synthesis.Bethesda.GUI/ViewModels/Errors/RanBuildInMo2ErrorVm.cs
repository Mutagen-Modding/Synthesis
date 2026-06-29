using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for RanBuildInMo2ErrorClassification
/// </summary>
public class RanBuildInMo2ErrorVm : ErrorClassificationVm
{
    private readonly RanBuildInMo2ErrorClassification _error;

    // Expose specific error properties for binding
    public string FilePath => _error.FilePath;

    /// <summary>
    /// Global settings VM for accessing BlockBuildingWithinMo2 setting
    /// </summary>
    public GlobalSettingsVm GlobalSettings { get; }

    /// <summary>
    /// Whether the "Enable Protection" section should be visible.
    /// Visible if BlockBuildingWithinMo2 was OFF at creation time.
    /// </summary>
    public bool ShowEnableProtection { get; }

    public delegate RanBuildInMo2ErrorVm Factory(RanBuildInMo2ErrorClassification error);

    public RanBuildInMo2ErrorVm(
        RanBuildInMo2ErrorClassification error,
        GlobalSettingsVm globalSettings,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        _error = error;
        GlobalSettings = globalSettings;

        // Show the option to enable protection if it wasn't already enabled
        ShowEnableProtection = !globalSettings.BlockBuildingWithinMo2;
    }
}
