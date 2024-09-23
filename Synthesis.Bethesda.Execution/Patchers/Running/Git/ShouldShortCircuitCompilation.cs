using Mutagen.Bethesda.Synthesis.Versioning;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public class ShouldShortCircuitCompilation
{
    private readonly IShortCircuitSettingsProvider _settingsProvider;
    private readonly IProvideCurrentVersions _provideCurrentVersions;

    public ShouldShortCircuitCompilation(
        IShortCircuitSettingsProvider settingsProvider,
        IBuildMetaFileReader metaFileReader,
        IProvideCurrentVersions provideCurrentVersions)
    {
        _settingsProvider = settingsProvider;
        _provideCurrentVersions = provideCurrentVersions;
    }

    public bool ShouldShortCircuit(RunnerRepoInfo info, GitCompilationMeta? meta)
    {
        if (!_settingsProvider.Shortcircuit) return false;
        if (meta == null) return false;
        if (meta.Sha != info.Target.TargetSha) return false;
        if (meta.MutagenVersion != info.TargetVersions.Mutagen) return false;
        if (meta.SynthesisVersion != info.TargetVersions.Synthesis) return false;
        if (meta.SynthesisUiVersion != _provideCurrentVersions.SynthesisVersion) return false;
        return true;
    }
}