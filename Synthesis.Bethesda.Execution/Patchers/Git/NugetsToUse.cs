using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Git;

public class NugetsToUse : IEquatable<NugetsToUse>
{
    public string Nickname { get; }
    public NugetVersioningEnum Versioning { get; }
    public string ManualVersion { get; }
    public string? NewestVersion { get; }

    public NugetsToUse(
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
        return obj is NugetsToUse versioning && Equals(versioning);
    }

    public bool Equals(NugetsToUse? other)
    {
        return other != null &&
               Versioning == other.Versioning &&
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

    public override string ToString()
    {
        return $"{Nickname} ==> Manual: {ManualVersion} Newest: {NewestVersion} ==> {Versioning}";
    }
}