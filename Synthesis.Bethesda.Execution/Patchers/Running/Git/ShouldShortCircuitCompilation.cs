using System.IO.Abstractions;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IShouldShortCircuitCompilation
{
    bool ShouldShortCircuit(RunnerRepoInfo info);
}

public class ShouldShortCircuitCompilation : IShouldShortCircuitCompilation
{
    private readonly IShortCircuitSettingsProvider _settingsProvider;
    private readonly IBuildMetaFileReader _metaFileReader;

    public ShouldShortCircuitCompilation(
        IShortCircuitSettingsProvider settingsProvider,
        IBuildMetaFileReader metaFileReader)
    {
        _settingsProvider = settingsProvider;
        _metaFileReader = metaFileReader;
    }

    public bool ShouldShortCircuit(RunnerRepoInfo info)
    {
        if (!_settingsProvider.Shortcircuit) return false;
        var meta = _metaFileReader.Read(info.MetaPath);
        if (meta == null) return false;
        if (meta.Sha != info.Target.TargetSha) return false;
        if (meta.MutagenVersion != info.TargetVersions.Mutagen) return false;
        if (meta.SynthesisVersion != info.TargetVersions.Synthesis) return false;
        return true;
    }
}