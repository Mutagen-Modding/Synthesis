using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Synthesis.Bethesda.DTO
{
    public enum PreferredAutoVersioning
    {
        /// <summary>
        /// Will perfer latest tag, if any tags exist
        /// </summary>
        [Description("Default")]
        Default,

        /// <summary>
        /// Uses latest tag for preferred automatic versioning
        /// </summary>
        [Description("Latest Tag")]
        LatestTag,

        /// <summary>
        /// Uses latest default branch for preferred automatic versioning
        /// </summary>
        [Description("Latest Default Branch")]
        LatestDefaultBranch,
    }

    public class PatcherCustomization : IEquatable<PatcherCustomization>
    {
        public string? Nickname { get; set; }
        public bool HideByDefault { get; set; } = false;
        public string? OneLineDescription { get; set; }
        public string? LongDescription { get; set; }
        public PreferredAutoVersioning PreferredAutoVersioning { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PatcherCustomization info && this.Equals(info);
        }

        public bool Equals(PatcherCustomization other)
        {
            if (HideByDefault != other.HideByDefault) return false;
            if (!string.Equals(this.OneLineDescription, other.OneLineDescription)) return false;
            if (!string.Equals(this.LongDescription, other.LongDescription)) return false;
            if (!string.Equals(this.Nickname, other.Nickname)) return false;
            if (!string.Equals(this.PreferredAutoVersioning, other.PreferredAutoVersioning)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(HideByDefault);
            hash.Add(OneLineDescription);
            hash.Add(LongDescription);
            hash.Add(Nickname);
            hash.Add(PreferredAutoVersioning);
            return hash.ToHashCode();
        }
    }
}
