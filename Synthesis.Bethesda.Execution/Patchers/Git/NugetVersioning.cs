using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using System;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class NugetVersioning : IEquatable<NugetVersioning>
    {
        public string Nickname { get; }
        public NugetVersioningEnum Versioning { get; }
        public string ManualVersion { get; }
        public string? NewestVersion { get; }

        public NugetVersioning(
            string nickname,
            NugetVersioningEnum versioning,
            string manualVersion,
            string? newestVersion)
        {
            Nickname = nickname;
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

        public GetResponse<string?> TryGetVersioning()
        {
            if (Versioning == NugetVersioningEnum.Latest && NewestVersion == null)
            {
                return GetResponse<string?>.Fail($"Latest {Nickname} version is desired, but latest version is not known.");
            }
            var ret = Versioning switch
            {
                NugetVersioningEnum.Latest => NewestVersion,
                NugetVersioningEnum.Match => null,
                NugetVersioningEnum.Manual => ManualVersion,
                _ => throw new NotImplementedException(),
            };
            if (Versioning == NugetVersioningEnum.Manual)
            {
                if (ret.IsNullOrWhitespace())
                {
                    return GetResponse<string?>.Fail($"Manual {Nickname} versioning had no input");
                }
            }
            return ret;
        }
    }
}
