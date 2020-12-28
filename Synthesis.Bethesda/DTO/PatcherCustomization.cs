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

    public enum VisibilityOptions
    {
        /// <summary>
        /// Completely excludes the patcher from being registered to Synthesis.
        /// </summary>
        [Description("Exclude")]
        Exclude,

        /// <summary>
        /// Includes patcher into Synthesis, but hides it by default.
        /// </summary>
        [Description("Include But Hide")]
        IncludeButHide,

        /// <summary>
        /// Patcher will always be visible.
        /// </summary>
        [Description("Always Visible")]
        Visible
    }

    public class PatcherCustomization : IEquatable<PatcherCustomization>
    {
        public string? Nickname { get; set; }
        public VisibilityOptions Visibility { get; set; } = VisibilityOptions.Visible;
        public string? OneLineDescription { get; set; }
        public string? LongDescription { get; set; }
        public PreferredAutoVersioning PreferredAutoVersioning { get; set; }
        public string[] RequiredMods { get; set; } = Array.Empty<string>();

        public override bool Equals(object? obj)
        {
            return obj is PatcherCustomization info && this.Equals(info);
        }

        public bool Equals(PatcherCustomization? other)
        {
            if (other == null) return false;
            if (!string.Equals(this.Visibility, other.Visibility)) return false;
            if (!string.Equals(this.OneLineDescription, other.OneLineDescription)) return false;
            if (!string.Equals(this.LongDescription, other.LongDescription)) return false;
            if (!string.Equals(this.Nickname, other.Nickname)) return false;
            if (!string.Equals(this.PreferredAutoVersioning, other.PreferredAutoVersioning)) return false;
            if (!RequiredMods.SequenceEqual(other.RequiredMods)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Visibility);
            hash.Add(OneLineDescription);
            hash.Add(LongDescription);
            hash.Add(Nickname);
            hash.Add(PreferredAutoVersioning);
            hash.Add(RequiredMods);
            return hash.ToHashCode();
        }
    }
}
