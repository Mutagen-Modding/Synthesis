using Noggog;
using System;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public record GithubPatcherLastRunState(
        string TargetRepo,
        string ProjectSubpath,
        string Commit, 
        string MutagenVersion,
        string SynthesisVersion);

    [ExcludeFromCodeCoverage]
    public class GithubPatcherSettings : PatcherSettings, IGithubPatcherIdentifier, IProjectSubpathDefaultSettings
    {
        public string ID = string.Empty;
        string IGithubPatcherIdentifier.Id => ID;
        public string RemoteRepoPath = string.Empty;
        public string SelectedProjectSubpath = string.Empty;
        public PatcherVersioningEnum PatcherVersioning = PatcherVersioningEnum.Branch;
        public string TargetTag = string.Empty;
        public string TargetCommit = string.Empty;
        public string TargetBranch = string.Empty;
        public bool LatestTag = true;
        public bool FollowDefaultBranch = true;
        public bool AutoUpdateToBranchTip = false;
        public bool OverrideNugetVersioning = false;
        public PatcherNugetVersioningEnum MutagenVersionType = PatcherNugetVersioningEnum.Profile;
        public string ManualMutagenVersion = string.Empty;
        public PatcherNugetVersioningEnum SynthesisVersionType = PatcherNugetVersioningEnum.Profile;
        public string ManualSynthesisVersion = string.Empty;
        public GithubPatcherLastRunState? LastSuccessfulRun;

        public override void Print(ILogger logger)
        {
            logger.Information($"[Git] {Nickname.Decorate(x => $"{x} => ")}{RemoteRepoPath}/{SelectedProjectSubpath} {PatcherVersioningString()}");
        }

        public string PatcherVersioningString()
        {
            switch (PatcherVersioning)
            {
                case PatcherVersioningEnum.Tag:
                    if (LatestTag)
                    {
                        return "Tag: Latest";
                    }
                    else
                    {
                        return $"Tag: {TargetTag}";
                    }
                case PatcherVersioningEnum.Branch:
                    if (FollowDefaultBranch)
                    {
                        return "Default Branch";
                    }
                    else
                    {
                        return $"Branch: {TargetBranch}";
                    }
                case PatcherVersioningEnum.Commit:
                    return $"Commit: {TargetCommit}";
                default:
                    throw new NotImplementedException();
            }
        }

        string IProjectSubpathProvider.ProjectSubpath => SelectedProjectSubpath;
    }
}
