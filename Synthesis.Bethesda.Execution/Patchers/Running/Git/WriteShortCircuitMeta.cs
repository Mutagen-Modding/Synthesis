using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.Versioning;
using Newtonsoft.Json;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IWriteShortCircuitMeta
{
    Task<GitCompilationMeta> WriteMeta(RunnerRepoInfo info, DotNetVersion dotNetVersion, CancellationToken cancel);
    void WriteMeta(string metaPath, GitCompilationMeta meta);
}

public class WriteShortCircuitMeta : IWriteShortCircuitMeta
{
    private readonly IFileSystem _fs;
    private readonly IProvideCurrentVersions _provideCurrentVersions;
    private readonly ILogger _logger;
    private readonly IQueryExecutablePath _queryExecutablePath;

    public WriteShortCircuitMeta(
        IFileSystem fs,
        IProvideCurrentVersions provideCurrentVersions,
        IQueryExecutablePath queryExecutablePath,
        ILogger logger)
    {
        _fs = fs;
        _provideCurrentVersions = provideCurrentVersions;
        _queryExecutablePath = queryExecutablePath;
        _logger = logger;
    }
        
    public async Task<GitCompilationMeta> WriteMeta(RunnerRepoInfo info, DotNetVersion dotNetVersion, CancellationToken cancel)
    {
        // Query the executable path that was just built
        string? executablePath = null;
        var execPathResult = await _queryExecutablePath.Query(info.Project.ProjPath, cancel).ConfigureAwait(false);
        if (execPathResult.Succeeded)
        {
            executablePath = execPathResult.Value;
            _logger.Information("Queried executable path: {Path}", executablePath);
        }
        else
        {
            _logger.Warning("Failed to query executable path: {Reason}", execPathResult.Reason);
        }


        var ret = new GitCompilationMeta()
        {
            NetSdkVersion = dotNetVersion.Version,
            SynthesisUiVersion = _provideCurrentVersions.SynthesisVersion,
            MutagenVersion = info.TargetVersions.Mutagen ?? string.Empty,
            SynthesisVersion = info.TargetVersions.Synthesis ?? string.Empty,
            Sha = info.Target.TargetSha,
            ExecutablePath = executablePath
        };
        WriteMeta(info.MetaPath, ret);
        return ret;
    }
        
    public void WriteMeta(string metaPath, GitCompilationMeta meta)
    {
        _logger.Information("Writing compilation meta path: {Path}.  Settings: {Settings}", metaPath, meta);
        _fs.File.WriteAllText(
            metaPath,
            JsonConvert.SerializeObject(meta));
    }
}