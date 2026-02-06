using System;
using System.Windows.Input;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

/// <summary>
/// View model wrapper for TooManyMastersError that provides profile settings integration
/// </summary>
public class TooManyMastersErrorVm : ErrorClassificationVm
{
    /// <summary>
    /// Minimum Synthesis version that supports the split masters feature
    /// </summary>
    public static readonly Version MinSplitSupportVersion = new(0, 36, 0);

    /// <summary>
    /// Documentation link for versioning setup
    /// </summary>
    public const string VersioningDocsLink = "https://mutagen-modding.github.io/Synthesis/Versioning/#recommended-setup";

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

    /// <summary>
    /// The Synthesis version the patcher is using, if available
    /// </summary>
    public string? PatcherSynthesisVersion { get; }

    /// <summary>
    /// Whether the patcher is using an old version that doesn't support split masters
    /// </summary>
    public bool IsOldPatcher { get; }

    /// <summary>
    /// Whether to show the "Older Patcher" warning section.
    /// Visible if SplitIfMaxMastersExceeded is ON but the patcher is old.
    /// </summary>
    public bool ShowOldPatcherWarning { get; }

    /// <summary>
    /// Command to navigate to the versioning documentation
    /// </summary>
    public ICommand VersioningDocsCommand { get; }

    public delegate TooManyMastersErrorVm Factory(TooManyMastersError error, PatcherVm? patcher);

    public TooManyMastersErrorVm(
        TooManyMastersError error,
        PatcherVm? patcher,
        ProfileVm profile,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        ProfileVm = profile;

        // Capture initial state to determine section visibility
        ShowPotentialFix = !profile.SplitIfMaxMastersExceeded;

        // Extract version info from the patcher if available
        PatcherSynthesisVersion = GetSynthesisVersion(patcher);
        IsOldPatcher = IsPatcherOld(PatcherSynthesisVersion);

        // Show old patcher warning if split is enabled but patcher is too old to support it
        ShowOldPatcherWarning = profile.SplitIfMaxMastersExceeded && IsOldPatcher;

        // Command to open the versioning documentation
        VersioningDocsCommand = ReactiveCommand.Create(() => navigateTo.Navigate(VersioningDocsLink));
    }

    private static string? GetSynthesisVersion(PatcherVm? patcher)
    {
        if (patcher is GitPatcherVm gitPatcher)
        {
            return gitPatcher.NugetDiff?.SynthesisVersionDiff?.SelectedVersion;
        }
        // For non-Git patchers, version may not be available
        return null;
    }

    private static bool IsPatcherOld(string? versionStr)
    {
        if (string.IsNullOrEmpty(versionStr)) return false;
        if (Version.TryParse(versionStr, out var version))
        {
            return version < MinSplitSupportVersion;
        }
        return false;
    }
}
