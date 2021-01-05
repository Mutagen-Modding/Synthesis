using Noggog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BaseSynthesis = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis
{
    public static class Versions
    {
        public static string MutagenVersion => FileVersionInfo.GetVersionInfo(typeof(FormKey).Assembly.Location)!.ProductVersion!.TrimEnd(".0").TrimEnd(".0");
        public static string SynthesisVersion => FileVersionInfo.GetVersionInfo(typeof(BaseSynthesis.Constants).Assembly.Location)!.ProductVersion!.TrimEnd(".0").TrimEnd(".0");
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
    }
}
