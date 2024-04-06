using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution;

public interface ISolutionPatcherPrep
{
    Task Prep(CancellationToken cancel);
}

public class SolutionPatcherPrep : ISolutionPatcherPrep
{
    private readonly ILogger _logger;
    public IPathToProjProvider PathToProjProvider { get; }
    public ICopyOverExtraData CopyOverExtraData { get; }
    public IBuild Build { get; }

    public SolutionPatcherPrep(
        ILogger logger,
        IPathToProjProvider pathToProjProvider,
        ICopyOverExtraData copyOverExtraData,
        IBuild build)
    {
        _logger = logger;
        PathToProjProvider = pathToProjProvider;
        CopyOverExtraData = copyOverExtraData;
        Build = build;
    }
        
    public async Task Prep(CancellationToken cancel)
    {
        await Task.WhenAll(
            Task.Run(async () =>
            {
                _logger.Information("Compiling solution {Path}", PathToProjProvider.Path);
                try
                {
                    var resp = await Build.Compile(PathToProjProvider.Path, cancel).ConfigureAwait(false);
                    if (!resp.Succeeded)
                    {
                        throw new SynthesisBuildFailure(resp.Reason);
                    }
                    _logger.Information("Compiled solution {Path}", PathToProjProvider.Path);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Failed to compile solution {PathToProjProvider.Path}");
                    throw;
                }
            }),
            Task.Run(() =>
            {
                CopyOverExtraData.Copy();
            })).ConfigureAwait(false);
    }
}