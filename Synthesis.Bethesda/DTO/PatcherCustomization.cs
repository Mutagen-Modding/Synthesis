using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.DTO
{
    public class PatcherCustomization : IEquatable<PatcherCustomization>
    {
        public string? Nickname { get; set; }
        public bool HideByDefault { get; set; } = false;
        public string? Description { get; set; }

        public override bool Equals(object obj)
        {
            return obj is PatcherCustomization info && this.Equals(info);
        }

        public bool Equals(PatcherCustomization other)
        {
            if (HideByDefault != other.HideByDefault) return false;
            if (!string.Equals(this.Description, other.Description)) return false;
            if (!string.Equals(this.Nickname, other.Nickname)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(HideByDefault);
            hash.Add(Description);
            hash.Add(Nickname);
            return hash.ToHashCode();
        }
    }
}
