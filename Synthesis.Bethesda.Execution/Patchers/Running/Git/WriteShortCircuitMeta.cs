using System.Collections.Concurrent;
using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.Versioning;
using Newtonsoft.Json;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IWriteShortCircuitMeta
{
    Task<GitCompilationMeta> WriteMeta(RunnerRepoInfo info, DotNetVersion dotNetVersion, CancellationToken cancel);
    void WriteMeta(string metaPath, GitCompilationMeta meta);

    /// <summary>
    /// Serialized read-modify-write, so a contributor merges into the latest on-disk meta rather
    /// than clobbering another flow's field with its own stale snapshot.
    /// </summary>
    void UpdateMeta(string metaPath, Func<GitCompilationMeta, GitCompilationMeta> update);
}

public class WriteShortCircuitMeta : IWriteShortCircuitMeta
{
    private static readonly ConcurrentDictionary<string, object> _metaLocks = new(StringComparer.OrdinalIgnoreCase);

    private static object LockFor(string metaPath) => _metaLocks.GetOrAdd(metaPath, _ => new object());

    private readonly IFileSystem _fs;
    private readonly IBuildMetaFileReader _metaFileReader;
    private readonly IProvideCurrentVersions _provideCurrentVersions;
    private readonly ILogger _logger;
    private readonly IQueryExecutablePath _queryExecutablePath;

    public WriteShortCircuitMeta(
        IFileSystem fs,
        IBuildMetaFileReader metaFileReader,
        IProvideCurrentVersions provideCurrentVersions,
        IQueryExecutablePath queryExecutablePath,
        ILogger logger)
    {
        _fs = fs;
        _metaFileReader = metaFileReader;
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
        lock (LockFor(metaPath))
        {
            WriteMetaUnsafe(metaPath, meta);
        }
    }

    public void UpdateMeta(string metaPath, Func<GitCompilationMeta, GitCompilationMeta> update)
    {
        lock (LockFor(metaPath))
        {
            var current = _metaFileReader.Read(metaPath);
            if (current == null)
            {
                _logger.Warning("Compilation meta missing at {Path} during update; skipping write", metaPath);
                return;
            }

            WriteMetaUnsafe(metaPath, update(current));
        }
    }

    private void WriteMetaUnsafe(string metaPath, GitCompilationMeta meta)
    {
        _logger.Information("Writing compilation meta path: {Path}.  Settings: {Settings}", metaPath, meta);
        var contents = JsonConvert.SerializeObject(meta);

        // Backstop for external readers that briefly hold the handle and surface as an IOException.
        const int maxAttempts = 5;
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                _fs.File.WriteAllText(metaPath, contents);
                return;
            }
            catch (IOException ex) when (attempt < maxAttempts)
            {
                _logger.Warning(ex,
                    "Failed to write compilation meta path {Path} (attempt {Attempt}/{MaxAttempts}); retrying",
                    metaPath, attempt, maxAttempts);
                Thread.Sleep(50 * attempt);
            }
        }
    }
}
