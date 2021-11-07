using System.IO.Abstractions;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git
{
    public interface IWriteShortCircuitMeta
    {
        void WriteMeta(RunnerRepoInfo info);
    }

    public class WriteShortCircuitMeta : IWriteShortCircuitMeta
    {
        private readonly IFileSystem _fs;

        public WriteShortCircuitMeta(IFileSystem fs)
        {
            _fs = fs;
        }
        
        public void WriteMeta(RunnerRepoInfo info)
        {
            _fs.File.WriteAllText(
                info.MetaPath,
                JsonConvert.SerializeObject(new GitCompilationMeta()
                {
                    MutagenVersion = info.TargetVersions.Mutagen ?? string.Empty,
                    SynthesisVersion = info.TargetVersions.Synthesis ?? string.Empty,
                    Sha = info.Target.TargetSha
                }));
        }
    }
}