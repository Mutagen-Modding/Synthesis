using System.Diagnostics;
using Mutagen.Bethesda.Plugins;
using Noggog;
using System.Reflection;
using BaseSynthesis = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.Versioning;

public static class Versions
{
    public static string MutagenVersion => GetVersion(typeof(FormKey).Assembly);
    public static string SynthesisVersion => GetVersion(typeof(BaseSynthesis.Constants).Assembly);
    public static string NewtonsoftVersion => GetVersion(typeof(Newtonsoft.Json.JsonConvert).Assembly);
    public static string OldMutagenVersion => "0.14.0";
    public static string OldSynthesisVersion => "0.0.3";
    public static string? MutagenSha => typeof(FormKey).Assembly.GetGitSha();
    public static string? SynthesisSha => typeof(BaseSynthesis.Constants).Assembly.GetGitSha();

    public static string? GetGitSha(this Assembly assemb)
    {
        var git = assemb.GetTypes().Where(x => x.FullName?.Equals("ThisAssembly+Git") ?? false).FirstOrDefault();
        if (git == null) return null;
        var str = git.GetField("Sha")?.GetValue(null) as string;
        if (str.IsNullOrWhitespace()) return null;
        return str;
    }

    public static string GetVersion(this Assembly assemb)
    {
        var version = FileVersionInfo.GetVersionInfo(assemb.Location).ProductVersion;
        return version!.TrimEnd(".0").TrimEnd(".0");
    }
}