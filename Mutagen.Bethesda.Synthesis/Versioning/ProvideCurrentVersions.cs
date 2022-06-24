using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mutagen.Bethesda.Plugins;
using Noggog;
using BaseSynthesis = Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.Versioning;

public interface IProvideCurrentVersions
{
    string SynthesisVersion { get; }
    string MutagenVersion { get; }
    public string NewtonsoftVersion { get; }
    public string OldMutagenVersion { get; }
    public string OldSynthesisVersion { get; }
    public string? MutagenSha { get; }
    public string? SynthesisSha { get; }
}
    
public sealed class ProvideCurrentVersions : IProvideCurrentVersions
{
    private readonly Lazy<string?> _mutagenSha;
    private readonly Lazy<string?> _synthesisSha;
    private readonly Lazy<string> _newtonsoftVersion;
    private readonly Lazy<string> _synthesisVersion;
    private readonly Lazy<string> _mutagenVersion;
    
    public string SynthesisVersion => _synthesisVersion.Value;

    public string MutagenVersion => _mutagenVersion.Value;

    public string NewtonsoftVersion => _newtonsoftVersion.Value;
    public string OldMutagenVersion => "0.14.0";
    public string OldSynthesisVersion => "0.0.3";
    public string? MutagenSha => _mutagenSha.Value;
    public string? SynthesisSha => _synthesisSha.Value;

    public ProvideCurrentVersions()
    {
        _mutagenSha = new Lazy<string?>(() => GetGitSha(typeof(FormKey).Assembly), LazyThreadSafetyMode.PublicationOnly);
        _synthesisSha = new Lazy<string?>(() => GetGitSha(typeof(BaseSynthesis.Constants).Assembly), LazyThreadSafetyMode.PublicationOnly);
        _mutagenVersion = new Lazy<string>(() => GetVersion(AssemblyVersions.For<FormKey>()), LazyThreadSafetyMode.PublicationOnly);
        _synthesisVersion = new Lazy<string>(() => GetVersion(AssemblyVersions.For<BaseSynthesis.Codes>()), LazyThreadSafetyMode.PublicationOnly);
        _newtonsoftVersion = new Lazy<string>(() => GetVersion(AssemblyVersions.For<Newtonsoft.Json.JsonSerializer>()), LazyThreadSafetyMode.PublicationOnly);
    }

    private string GetVersion(AssemblyVersions versions)
    {
        try
        {
            var version = versions.ProductVersion;
            return version!.TrimEnd(".0").TrimEnd(".0");
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error retrieving product version for {versions.PrettyName}",
                e);
        }
    }

    private string? GetGitSha(Assembly assemb)
    {
        try
        {
            var git = assemb.GetTypes().Where(x => x.FullName?.Equals("ThisAssembly+Git") ?? false).FirstOrDefault();
            if (git == null) return null;
            var str = git.GetField("Sha")?.GetValue(null) as string;
            if (str.IsNullOrWhitespace()) return null;
            return str;
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error retrieving git sha for {assemb.FullName}",
                e);
        }
    }
}