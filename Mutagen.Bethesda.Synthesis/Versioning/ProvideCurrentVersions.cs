using System.Reflection;
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
    IEnumerable<string> GetVersionPrintouts();
}
    
public sealed class ProvideCurrentVersions : IProvideCurrentVersions
{
    private record VersionQuery(
        string? MutagenSha,
        string? SynthesisSha,
        string NewtonsoftVersion,
        string SynthesisVersion,
        string MutagenVersion);
    
    private readonly Lazy<VersionQuery> _versionQuery;
    
    public string SynthesisVersion => _versionQuery.Value.SynthesisVersion;

    public string MutagenVersion => _versionQuery.Value.MutagenVersion;

    public string NewtonsoftVersion => _versionQuery.Value.NewtonsoftVersion;
    public string OldMutagenVersion => "0.14.0";
    public string OldSynthesisVersion => "0.0.3";
    public string? MutagenSha => _versionQuery.Value.MutagenSha;
    public string? SynthesisSha => _versionQuery.Value.SynthesisSha;

    public ProvideCurrentVersions()
    {
        _versionQuery = new Lazy<VersionQuery>(() =>
        {
            return new VersionQuery(
                MutagenSha: GetGitSha(typeof(FormKey).Assembly),
                SynthesisSha: GetGitSha(typeof(BaseSynthesis.Constants).Assembly),
                MutagenVersion: GetVersion(typeof(FormKey)),
                SynthesisVersion: GetVersion(AssemblyVersions.For<BaseSynthesis.Codes>()),
                NewtonsoftVersion: GetVersion(AssemblyVersions.For<Newtonsoft.Json.JsonSerializer>()));

        }, LazyThreadSafetyMode.ExecutionAndPublication);
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

    private string GetVersion(Type t)
    {
        try
        {
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(t.Assembly.Location);
            
            return fvi.FileVersion ?? "Unknown";
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error retrieving product version for {t}",
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

    public IEnumerable<string> GetVersionPrintouts()
    {
        yield return $"Mutagen version: {MutagenVersion}";
        yield return $"Mutagen sha: {MutagenSha}";
        yield return $"Synthesis version: {SynthesisVersion}";
        yield return $"Synthesis sha: {SynthesisSha}";
        yield return $"Newtonsoft version: {NewtonsoftVersion}";
    }
}