using Noggog;
using Synthesis.Bethesda.Execution.DotNet.Builder.Transient;
using Synthesis.Bethesda.Execution.Utility;
using Noggog.WorkEngine;
using Serilog;

namespace Synthesis.Bethesda.Execution.DotNet.Builder;

public interface IBuild
{
    Task<ErrorResponse> Compile(FilePath targetPath, CancellationToken cancel);
}

public class Build : IBuild
{
    private readonly ILogger _logger;
    public IWorkDropoff Dropoff { get; }
    public Func<IBuildOutputAccumulator> OutputAccumulatorFactory { get; }
    public IBuildResultsProcessor ResultsProcessor { get; }
    public ISynthesisSubProcessRunner ProcessRunner { get; }
    public IBuildStartInfoProvider BuildStartInfoProvider { get; }

    public Build(
        ISynthesisSubProcessRunner processRunner,
        IWorkDropoff workDropoff,
        Func<IBuildOutputAccumulator> outputAccumulatorFactory,
        IBuildResultsProcessor resultsProcessor,
        IBuildStartInfoProvider buildStartInfoProvider, 
        ILogger logger)
    {
        Dropoff = workDropoff;
        OutputAccumulatorFactory = outputAccumulatorFactory;
        ResultsProcessor = resultsProcessor;
        ProcessRunner = processRunner;
        BuildStartInfoProvider = buildStartInfoProvider;
        _logger = logger;
    }
        
    public async Task<ErrorResponse> Compile(FilePath targetPath, CancellationToken cancel)
    {
        _logger.Information("Preparing to build {TargetPath}", targetPath);
        var start = BuildStartInfoProvider.Construct(targetPath.Name.ToString());
        start.WorkingDirectory = targetPath.Directory!;

        var accumulator = OutputAccumulatorFactory();

        _logger.Information("Queuing build for {TargetPath}", targetPath);
        var result = await Dropoff.EnqueueAndWait(async () =>
        {
            _logger.Information("Starting build for {TargetPath}", targetPath);
            var ret = await ProcessRunner.RunWithCallback(
                start,
                outputCallback: accumulator.Process,
                errorCallback: e => {},
                cancel: cancel);
            _logger.Information("Finished build for {TargetPath}", targetPath);
            return ret;
        }, cancel).ConfigureAwait(false);
            
        if (result == 0) return ErrorResponse.Success;

        return ResultsProcessor.GetResults(
            targetPath,
            result,
            cancel,
            accumulator);
    }
}