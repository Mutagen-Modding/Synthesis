using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class SynthesisNugetVersioning : IEquatable<SynthesisNugetVersioning>
    {
        public NugetVersioning Mutagen { get; }
        public NugetVersioning Synthesis { get; }

        public SynthesisNugetVersioning(NugetVersioning mutagen, NugetVersioning synthesis)
        {
            Mutagen = mutagen;
            Synthesis = synthesis;
        }

        public override bool Equals(object obj)
        {
            return obj is SynthesisNugetVersioning vers && Equals(vers);
        }

        public bool Equals(SynthesisNugetVersioning other)
        {
            if (!Mutagen.Equals(other.Mutagen)) return false;
            if (!Synthesis.Equals(other.Synthesis)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Mutagen, Synthesis);
        }

        public GetResponse<NugetVersioningTarget> TryGetTarget()
        {
            if (this.Mutagen.Versioning == NugetVersioningEnum.Latest && this.Mutagen.NewestVersion == null)
            {
                return GetResponse<NugetVersioningTarget>.Fail("Latest Mutagen version is desired, but latest version is not known.");
            }
            if (this.Synthesis.Versioning == NugetVersioningEnum.Latest && this.Synthesis.NewestVersion == null)
            {
                return GetResponse<NugetVersioningTarget>.Fail("Latest Synthesis version is desired, but latest version is not known.");
            }
            var muta = this.Mutagen.Versioning switch
            {
                NugetVersioningEnum.Latest => (this.Mutagen.NewestVersion, this.Mutagen.Versioning),
                NugetVersioningEnum.Match => (null, this.Mutagen.Versioning),
                NugetVersioningEnum.Manual => (this.Mutagen.ManualVersion, this.Mutagen.Versioning),
                _ => throw new NotImplementedException(),
            };
            var synth = this.Synthesis.Versioning switch
            {
                NugetVersioningEnum.Latest => (this.Synthesis.NewestVersion, this.Synthesis.Versioning),
                NugetVersioningEnum.Match => (null, this.Synthesis.Versioning),
                NugetVersioningEnum.Manual => (this.Synthesis.ManualVersion, this.Synthesis.Versioning),
                _ => throw new NotImplementedException(),
            };
            if (muta.Versioning == NugetVersioningEnum.Manual)
            {
                if (muta.Item1.IsNullOrWhitespace())
                {
                    return GetResponse<NugetVersioningTarget>.Fail("Manual Mutagen versioning had no input");
                }
            }
            if (synth.Versioning == NugetVersioningEnum.Manual)
            {
                if (synth.Item1.IsNullOrWhitespace())
                {
                    return GetResponse<NugetVersioningTarget>.Fail("Manual Synthesis versioning had no input");
                }
            }
            return GetResponse<NugetVersioningTarget>.Succeed(new NugetVersioningTarget(muta.Item1, muta.Versioning, synth.Item1, synth.Versioning));
        }
    }
}
