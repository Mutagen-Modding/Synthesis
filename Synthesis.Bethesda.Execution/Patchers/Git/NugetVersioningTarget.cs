using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class NugetVersioningTarget : IEquatable<NugetVersioningTarget>
    {
        public string? MutagenVersion { get; }
        public NugetVersioningEnum MutagenVersioning { get; }
        public string? SynthesisVersion { get; }
        public NugetVersioningEnum SynthesisVersioning { get; }

        public NugetVersioningTarget(
            string? mutagenVersion,
            NugetVersioningEnum mutagenVersioning,
            string? synthesisVersion,
            NugetVersioningEnum synthesisVersioning)
        {
            MutagenVersion = mutagenVersion;
            MutagenVersioning = mutagenVersioning;
            SynthesisVersion = synthesisVersion;
            SynthesisVersioning = synthesisVersioning;
        }

        public override bool Equals(object? obj)
        {
            return obj is NugetVersioningTarget target && Equals(target);
        }

        public bool Equals(NugetVersioningTarget other)
        {
            return MutagenVersion == other.MutagenVersion &&
                   MutagenVersioning == other.MutagenVersioning &&
                   SynthesisVersion == other.SynthesisVersion &&
                   SynthesisVersioning == other.SynthesisVersioning;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MutagenVersion, MutagenVersioning, SynthesisVersion, SynthesisVersioning);
        }
    }
}
