using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class NugetVersioning : IEquatable<NugetVersioning>
    {
        public NugetVersioningEnum Versioning { get; }
        public string ManualVersion { get; }
        public string? NewestVersion { get; }

        public NugetVersioning(
            NugetVersioningEnum versioning,
            string manualVersion,
            string? newestVersion)
        {
            Versioning = versioning;
            ManualVersion = manualVersion;
            NewestVersion = newestVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is NugetVersioning versioning && Equals(versioning);
        }

        public bool Equals(NugetVersioning other)
        {
            return Versioning == other.Versioning &&
                ManualVersion == other.ManualVersion &&
                NewestVersion == other.NewestVersion;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Versioning,
                ManualVersion,
                NewestVersion);
        }
    }
}
