﻿using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherCompilation
{
    Task<ErrorResponse> Compile(
        RunnerRepoInfo info,
        DotNetVersion dotNetVersion,
        CancellationToken cancel);
}

public class GitPatcherCompilation : IGitPatcherCompilation
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fs;
    private readonly IBuild _build;
    private readonly IWriteShortCircuitMeta _writeShortCircuitMeta;
    private readonly ShouldShortCircuitCompilation _shortCircuitCompilation;
    private readonly BuildDirectoryCleaner _buildDirectoryCleaner;
    private readonly IBuildMetaFileReader _metaFileReader;

    public GitPatcherCompilation(
        ILogger logger,
        IFileSystem fs,
        IBuild build,
        IWriteShortCircuitMeta writeShortCircuitMeta,
        ShouldShortCircuitCompilation shortCircuitCompilation,
        BuildDirectoryCleaner buildDirectoryCleaner,
        IBuildMetaFileReader metaFileReader)
    {
        _logger = logger;
        _fs = fs;
        _build = build;
        _writeShortCircuitMeta = writeShortCircuitMeta;
        _shortCircuitCompilation = shortCircuitCompilation;
        _buildDirectoryCleaner = buildDirectoryCleaner;
        _metaFileReader = metaFileReader;
    }
        
    public async Task<ErrorResponse> Compile(
        RunnerRepoInfo info,
        DotNetVersion dotNetVersion,
        CancellationToken cancel)
    {
        try
        {
            var meta = _metaFileReader.Read(info.MetaPath);
            if (_shortCircuitCompilation.ShouldShortCircuit(info, meta))
            {
                _logger.Information("Short circuiting {Path} compilation because meta matched", info.Project.ProjPath);
                return ErrorResponse.Success;
            }
            
            _buildDirectoryCleaner.Clean(info, dotNetVersion, meta);
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed when checking if we could short circuit compilation");
        }

        try
        {
            _logger.Information("Clearing short circuit meta");
            _fs.File.Delete(info.MetaPath);
            _logger.Information("Cleared short circuit meta");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed when clearing short circuit meta");
        }
        
        var resp = await _build.Compile(info.Project.ProjPath, cancel).ConfigureAwait(false);
        if (resp.Failed) return resp;

        try
        {
            _writeShortCircuitMeta.WriteMeta(info, dotNetVersion);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed when saving short circuit meta");
            return ErrorResponse.Fail(e);
        }

        return resp;
    }
}