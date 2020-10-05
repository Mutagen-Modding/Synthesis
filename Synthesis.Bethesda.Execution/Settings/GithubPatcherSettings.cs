using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public enum PatcherVersioningEnum
    {
        Master,
        Tag,
        Branch,
        Commit,
    }

    public enum MutagenVersioningEnum
    {
        Match,
        Latest,
        Manual,
    }

    public class GithubPatcherSettings : PatcherSettings
    {
        public string ID = string.Empty;
        public string RemoteRepoPath = string.Empty;
        public string SelectedProjectSubpath = string.Empty;
        public PatcherVersioningEnum PatcherVersioning = PatcherVersioningEnum.Master;
        public MutagenVersioningEnum MutagenVersioning = MutagenVersioningEnum.Match;
        public string TargetTag = string.Empty;
        public string TargetCommit = string.Empty;
        public string TargetBranch = string.Empty;
        public string ExtraDataPath = string.Empty;
    }
}
