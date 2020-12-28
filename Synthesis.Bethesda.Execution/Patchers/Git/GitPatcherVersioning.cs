using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class GitPatcherVersioning : IEquatable<GitPatcherVersioning>
    {
        public PatcherVersioningEnum Versioning { get; }
        public string Target { get; }

        public GitPatcherVersioning(
            PatcherVersioningEnum versioning,
            string target)
        {
            Versioning = versioning;
            Target = target;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is GitPatcherVersioning rhs)) return false;
            return Equals(rhs);
        }

        public bool Equals(GitPatcherVersioning? other)
        {
            if (other == null) return false;
            if (Versioning != other.Versioning) return false;
            if (!string.Equals(Target, other.Target)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            HashCode code = new HashCode();
            code.Add(Versioning);
            code.Add(Target);
            return code.ToHashCode();
        }

        public override string ToString()
        {
            return Versioning switch
            {
                PatcherVersioningEnum.Tag => $"tag {Target}",
                PatcherVersioningEnum.Branch => $"branch {Target}",
                PatcherVersioningEnum.Commit => $"commit {Target}",
                _ => throw new NotImplementedException(),
            };
        }

        public static GitPatcherVersioning Factory(
            PatcherVersioningEnum versioning,
            string tag,
            string commit,
            string branch,
            bool autoTag,
            bool autoBranch)
        {
            switch (versioning)
            {
                case PatcherVersioningEnum.Tag:
                    if (!autoTag)
                    {
                        versioning = PatcherVersioningEnum.Commit;
                    }
                    break;
                case PatcherVersioningEnum.Branch:
                    if (!autoBranch)
                    {
                        versioning = PatcherVersioningEnum.Commit;
                    }
                    break;
                default:
                    break;
            }
            return new GitPatcherVersioning(
                versioning,
                versioning switch
                {
                    PatcherVersioningEnum.Branch => branch,
                    PatcherVersioningEnum.Tag => tag,
                    PatcherVersioningEnum.Commit => commit,
                    _ => throw new NotImplementedException(),
                });
        }
    }
}
