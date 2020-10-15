using Noggog;
using Synthesis.Bethesda.Execution.Reporters;
using System;

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
        Latest,
        Match,
        Manual,
    }

    public enum SynthesisVersioningEnum
    {
        Latest,
        Match,
        Manual,
    }

    public class GithubPatcherSettings : PatcherSettings
    {
        public string ID = string.Empty;
        public string RemoteRepoPath = string.Empty;
        public string SelectedProjectSubpath = string.Empty;
        public PatcherVersioningEnum PatcherVersioning = PatcherVersioningEnum.Master;
        public string TargetTag = string.Empty;
        public string TargetCommit = string.Empty;
        public string TargetBranch = string.Empty;
        public MutagenVersioningEnum MutagenVersioning = MutagenVersioningEnum.Latest;
        public string ManualMutagenVersion = string.Empty;
        public SynthesisVersioningEnum SynthesisVersioning = SynthesisVersioningEnum.Latest;
        public string ManualSynthesisVersion = string.Empty;

        public override void Print(IRunReporter logger)
        {
            logger.Write(default, $"[Git] {Nickname.Decorate(x => $"{x} => ")}{RemoteRepoPath}/{SelectedProjectSubpath} {PatcherVersioningString()}");
        }

        public string PatcherVersioningString()
        {
            return PatcherVersioning switch
            {
                PatcherVersioningEnum.Master => $"Master Tracking",
                PatcherVersioningEnum.Tag => $"Tag: {TargetTag}",
                PatcherVersioningEnum.Branch => $"Branch: {TargetBranch}",
                PatcherVersioningEnum.Commit => $"Commit: {TargetCommit}",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
