using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// View model wrapper for TooManyMastersError that provides profile settings integration
/// </summary>
public class TooManyMastersErrorVm : ErrorClassificationVm
{
    /// <summary>
    /// The profile VM for accessing settings
    /// </summary>
    public ProfileVm ProfileVm { get; }

    /// <summary>
    /// Whether the "Potential Fix" section should be visible.
    /// Visible if SplitIfMaxMastersExceeded was OFF at creation time.
    /// Once visible, stays visible regardless of toggle state.
    /// </summary>
    public bool ShowPotentialFix { get; }

    public delegate TooManyMastersErrorVm Factory(TooManyMastersError error);

    public TooManyMastersErrorVm(
        TooManyMastersError error,
        ProfileVm profile,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        ProfileVm = profile;

        // Capture initial state to determine section visibility
        ShowPotentialFix = !profile.SplitIfMaxMastersExceeded;
    }
}
