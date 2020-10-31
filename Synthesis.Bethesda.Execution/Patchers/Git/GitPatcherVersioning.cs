using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class GitPatcherVersioning : IEquatable<GitPatcherVersioning>
    {
        public PatcherVersioningEnum Versioning { get; }
        public string TargetTag { get; }
        public string TargetCommit { get; }
        public string TargetBranchName { get; }

        public GitPatcherVersioning(
            PatcherVersioningEnum versioning,
            string targetTag,
            string targetCommit,
            string targetBranch)
        {
            Versioning = versioning;
            TargetTag = targetTag;
            TargetCommit = targetCommit;
            TargetBranchName = targetBranch;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GitPatcherVersioning rhs)) return false;
            return Equals(rhs);
        }

        public bool Equals(GitPatcherVersioning other)
        {
            if (Versioning != other.Versioning) return false;
            if (!string.Equals(TargetTag, other.TargetTag)) return false;
            if (!string.Equals(TargetCommit, other.TargetCommit)) return false;
            if (!string.Equals(TargetBranchName, other.TargetBranchName)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            HashCode code = new HashCode();
            code.Add(Versioning);
            code.Add(TargetTag);
            code.Add(TargetCommit);
            code.Add(TargetBranchName);
            return code.ToHashCode();
        }
    }
}
