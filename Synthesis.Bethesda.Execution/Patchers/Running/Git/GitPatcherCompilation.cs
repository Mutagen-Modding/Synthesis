using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public interface IGitPatcherCompilation
{
    Task<ErrorResponse> Compile(RunnerRepoInfo info, CancellationToken cancel);
}

public class GitPatcherCompilation : IGitPatcherCompilation
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fs;
    private readonly IBuild _build;
    private readonly IWriteShortCircuitMeta _writeShortCircuitMeta;
    private readonly IShouldShortCircuitCompilation _shortCircuitCompilation;

    public GitPatcherCompilation(
        ILogger logger,
        IFileSystem fs,
        IBuild build,
        IWriteShortCircuitMeta writeShortCircuitMeta,
        IShouldShortCircuitCompilation shortCircuitCompilation)
    {
        _logger = logger;
        _fs = fs;
        _build = build;
        _writeShortCircuitMeta = writeShortCircuitMeta;
        _shortCircuitCompilation = shortCircuitCompilation;
    }
        
    public async Task<ErrorResponse> Compile(RunnerRepoInfo info, CancellationToken cancel)
    {
        try
        {
            if (_shortCircuitCompilation.ShouldShortCircuit(info))
            {
                _logger.Information("Short circuiting {Path} compilation because meta matched", info.ProjPath);
                return ErrorResponse.Success;
            }
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed when checking if we could short circuit compilation");
        }

        try
        {
            _logger.Information("Clearing short circuit meta");
            _fs.File.Delete(info.MetaPath);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed when clearing short circuit meta");
        }
            
        var resp = await _build.Compile(info.ProjPath, cancel).ConfigureAwait(false);
        if (resp.Failed) return resp;

        try
        {
            _writeShortCircuitMeta.WriteMeta(info);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed when saving short circuit meta");
            return ErrorResponse.Fail(e);
        }

        return resp;
    }
}