using System.IO.Abstractions;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IWriteShortCircuitMeta
{
    void WriteMeta(RunnerRepoInfo info);
    void WriteMeta(string metaPath, GitCompilationMeta meta);
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
        WriteMeta(info.MetaPath, new GitCompilationMeta()
        {
            MutagenVersion = info.TargetVersions.Mutagen ?? string.Empty,
            SynthesisVersion = info.TargetVersions.Synthesis ?? string.Empty,
            Sha = info.Target.TargetSha
        });
    }
        
    public void WriteMeta(string metaPath, GitCompilationMeta meta)
    {
        _fs.File.WriteAllText(
            metaPath,
            JsonConvert.SerializeObject(meta));
    }
}