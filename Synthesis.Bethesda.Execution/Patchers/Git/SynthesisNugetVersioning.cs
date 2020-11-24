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
            var mutaVersion = this.Mutagen.TryGetVersioning(); 
            if (mutaVersion.Failed) mutaVersion.BubbleFailure<NugetVersioningTarget>();
            var synthesisVersion = this.Synthesis.TryGetVersioning();
            if (synthesisVersion.Failed) synthesisVersion.BubbleFailure<NugetVersioningTarget>();
            return GetResponse<NugetVersioningTarget>.Succeed(new NugetVersioningTarget(mutaVersion.Value, this.Mutagen.Versioning, synthesisVersion.Value, this.Synthesis.Versioning));
        }
    }
}
