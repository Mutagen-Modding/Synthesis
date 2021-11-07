using System.IO.Abstractions;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git
{
    public interface IShouldShortCircuitCompilation
    {
        bool ShouldShortCircuit(RunnerRepoInfo info);
    }

    public class ShouldShortCircuitCompilation : IShouldShortCircuitCompilation
    {
        private readonly IShortCircuitCompilationSettingsProvider _settingsProvider;
        private readonly IFileSystem _fs;

        public ShouldShortCircuitCompilation(
            IShortCircuitCompilationSettingsProvider settingsProvider,
            IFileSystem fs)
        {
            _settingsProvider = settingsProvider;
            _fs = fs;
        }

        public bool ShouldShortCircuit(RunnerRepoInfo info)
        {
            if (!_settingsProvider.ShortcircuitBuilds) return false;
            if (!_fs.File.Exists(info.MetaPath)) return false;
            var meta = JsonConvert.DeserializeObject<GitCompilationMeta>(_fs.File.ReadAllText(info.MetaPath), Constants.JsonSettings)!;
            if (meta.Sha != info.Target.TargetSha) return false;
            if (meta.MutagenVersion != info.TargetVersions.Mutagen) return false;
            if (meta.SynthesisVersion != info.TargetVersions.Synthesis) return false;
            return true;
        }
    }
}