using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.Versioning;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public class ShouldShortCircuitCompilation
{
    private readonly IShortCircuitSettingsProvider _settingsProvider;
    private readonly IProvideCurrentVersions _provideCurrentVersions;
    private readonly IFileSystem _fs;

    public ShouldShortCircuitCompilation(
        IShortCircuitSettingsProvider settingsProvider,
        IBuildMetaFileReader metaFileReader,
        IProvideCurrentVersions provideCurrentVersions,
        IFileSystem fs)
    {
        _settingsProvider = settingsProvider;
        _provideCurrentVersions = provideCurrentVersions;
        _fs = fs;
    }

public bool ShouldShortCircuit(RunnerRepoInfo info, GitCompilationMeta? meta)
    {
        if (!_settingsProvider.Shortcircuit) return false;
        if (meta == null) return false;
        if (meta.Sha != info.Target.TargetSha) return false;
        if (meta.MutagenVersion != info.TargetVersions.Mutagen) return false;
        if (meta.SynthesisVersion != info.TargetVersions.Synthesis) return false;
        if (meta.SynthesisUiVersion != _provideCurrentVersions.SynthesisVersion) return false;
        
        // Check if executable path exists and is valid
        if (string.IsNullOrWhiteSpace(meta.ExecutablePath)) return false;
        if (!_fs.File.Exists(meta.ExecutablePath)) return false;
        
        return true;
    }
}