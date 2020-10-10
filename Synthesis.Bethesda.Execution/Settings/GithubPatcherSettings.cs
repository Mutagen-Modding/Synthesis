using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
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

        public override void Print(ILogger logger)
        {
            logger.Write($"[Git] {Nickname.Decorate(x => $"{x} => ")}{RemoteRepoPath}/{SelectedProjectSubpath} {PatcherVersioningString()}");
        }

        public string PatcherVersioningString()
        {
            return PatcherVersioning switch
            {
                PatcherVersioningEnum.Master => $"Master Track",
                PatcherVersioningEnum.Tag => $"Tag: {TargetTag}",
                PatcherVersioningEnum.Branch => $"Branch: {TargetBranch}",
                PatcherVersioningEnum.Commit => $"Commit: {TargetCommit}",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
