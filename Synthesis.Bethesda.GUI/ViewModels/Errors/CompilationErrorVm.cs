using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Errors;

/// <summary>
/// View model wrapper for CompilationErrorClassification.
/// Provides patcher versioning context and quick-fix actions.
/// </summary>
public class CompilationErrorVm : ErrorClassificationVm
{
    public delegate CompilationErrorVm Factory(CompilationErrorClassification error, PatcherVm? patcher);

    /// <summary>
    /// The patcher VM if available
    /// </summary>
    public PatcherVm? PatcherVm { get; }

    /// <summary>
    /// Whether the patcher is a Git patcher with its own versioning settings
    /// </summary>
    public bool IsGitPatcher { get; }

    /// <summary>
    /// The actual compilation error text from the build failure
    /// </summary>
    public string CompilationText { get; }

    /// <summary>
    /// Sets both Mutagen and Synthesis versioning to Match
    /// </summary>
    public ICommand SetToMatchCommand { get; }

    /// <summary>
    /// Opens the patcher's repo issues page
    /// </summary>
    public ICommand OpenIssueCommand { get; }

    public CompilationErrorVm(
        CompilationErrorClassification error,
        PatcherVm? patcher,
        INavigateTo navigateTo)
        : base(error, navigateTo)
    {
        PatcherVm = patcher;
        IsGitPatcher = patcher is GitPatcherVm;
        CompilationText = error.CompilationText;

        var gitPatcher = patcher as GitPatcherVm;

        SetToMatchCommand = ReactiveCommand.Create(() =>
        {
            if (gitPatcher == null) return;
            gitPatcher.NugetTargeting.MutagenVersioning = PatcherNugetVersioningEnum.Match;
            gitPatcher.NugetTargeting.SynthesisVersioning = PatcherNugetVersioningEnum.Match;
        });

        OpenIssueCommand = ReactiveCommand.Create(
            () =>
            {
                if (gitPatcher == null) return;
                var repoUrl = gitPatcher.RemoteRepoPathInput.RemoteRepoPath;
                if (!string.IsNullOrEmpty(repoUrl))
                {
                    _navigateTo.Navigate(repoUrl.TrimEnd('/') + "/issues");
                }
            });
    }
}
