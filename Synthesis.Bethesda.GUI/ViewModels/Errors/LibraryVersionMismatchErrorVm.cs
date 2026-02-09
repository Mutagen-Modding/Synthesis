using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for LibraryVersionMismatchErrorClassification
/// Provides profile and patcher versioning context for display and editing
/// </summary>
public class LibraryVersionMismatchErrorVm : ErrorClassificationVm
{
    public delegate LibraryVersionMismatchErrorVm Factory(LibraryVersionMismatchErrorClassification error, PatcherVm? patcher);

    /// <summary>
    /// The profile VM for accessing versioning settings
    /// </summary>
    public ProfileVm ProfileVm { get; }

    /// <summary>
    /// The patcher VM if available
    /// </summary>
    public PatcherVm? PatcherVm { get; }

    /// <summary>
    /// Whether the patcher is a Git patcher with its own versioning settings
    /// </summary>
    public bool IsGitPatcher { get; }

    public LibraryVersionMismatchErrorVm(
        LibraryVersionMismatchErrorClassification error,
        PatcherVm? patcher,
        ProfileVm profile,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        ProfileVm = profile;
        PatcherVm = patcher;
        IsGitPatcher = patcher is GitPatcherVm;
    }
}
