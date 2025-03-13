using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.Versioning;
using Newtonsoft.Json;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IWriteShortCircuitMeta
{
    void WriteMeta(RunnerRepoInfo info, DotNetVersion dotNetVersion);
    void WriteMeta(string metaPath, GitCompilationMeta meta);
}

public class WriteShortCircuitMeta : IWriteShortCircuitMeta
{
    private readonly IFileSystem _fs;
    private readonly IProvideCurrentVersions _provideCurrentVersions;
    private readonly ILogger _logger;

    public WriteShortCircuitMeta(
        IFileSystem fs, 
        IProvideCurrentVersions provideCurrentVersions,
        ILogger logger)
    {
        _fs = fs;
        _provideCurrentVersions = provideCurrentVersions;
        _logger = logger;
    }
        
    public void WriteMeta(RunnerRepoInfo info, DotNetVersion dotNetVersion)
    {
        WriteMeta(info.MetaPath, new GitCompilationMeta()
        {
            NetSdkVersion = dotNetVersion.Version,
            SynthesisUiVersion = _provideCurrentVersions.SynthesisVersion,
            MutagenVersion = info.TargetVersions.Mutagen ?? string.Empty,
            SynthesisVersion = info.TargetVersions.Synthesis ?? string.Empty,
            Sha = info.Target.TargetSha
        });
    }
        
    public void WriteMeta(string metaPath, GitCompilationMeta meta)
    {
        _logger.Information("Writing compilation meta path: {Path}.  Settings: {Settings}", metaPath, meta);
        _fs.File.WriteAllText(
            metaPath,
            JsonConvert.SerializeObject(meta));
    }
}