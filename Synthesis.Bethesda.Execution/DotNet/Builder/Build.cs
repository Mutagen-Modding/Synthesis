using Noggog;
using Synthesis.Bethesda.Execution.DotNet.Builder.Transient;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
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
    private readonly IMo2EnvironmentDetector _mo2Detector;
    private readonly IBlockBuildingWithinMo2SettingsProvider _mo2BuildBlockSettings;
    private readonly IBuildLock _buildLock;
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
        IMo2EnvironmentDetector mo2Detector,
        IBlockBuildingWithinMo2SettingsProvider mo2BuildBlockSettings,
        IBuildLock buildLock,
        ILogger logger)
    {
        Dropoff = workDropoff;
        OutputAccumulatorFactory = outputAccumulatorFactory;
        ResultsProcessor = resultsProcessor;
        ProcessRunner = processRunner;
        BuildStartInfoProvider = buildStartInfoProvider;
        _mo2Detector = mo2Detector;
        _mo2BuildBlockSettings = mo2BuildBlockSettings;
        _buildLock = buildLock;
        _logger = logger;
    }

    public async Task<ErrorResponse> Compile(FilePath targetPath, CancellationToken cancel)
    {
        if (_mo2BuildBlockSettings.BlockBuildingWithinMo2 && _mo2Detector.IsRunningInsideMo2())
        {
            throw new Mo2BuildBlockedException();
        }

        _logger.Information("Preparing to build {TargetPath}", targetPath);
        var start = BuildStartInfoProvider.Construct(targetPath.Name.ToString());
        start.WorkingDirectory = targetPath.Directory!;

        var accumulator = OutputAccumulatorFactory();

        _logger.Information("Queuing build for {TargetPath}", targetPath);
        var result = await Dropoff.EnqueueAndWait(async () =>
        {
            using (await _buildLock.GetLock(targetPath).WaitAsync())
            {
                _logger.Information("Starting build for {TargetPath}", targetPath);
                var ret = await ProcessRunner.RunWithCallback(
                    start,
                    outputCallback: accumulator.Process,
                    errorCallback: e => {},
                    cancel: cancel);
                _logger.Information("Finished build for {TargetPath}", targetPath);
                return ret;
            }
        }, cancel).ConfigureAwait(false);
            
        if (result == 0) return ErrorResponse.Success;

        return ResultsProcessor.GetResults(
            targetPath,
            result,
            cancel,
            accumulator);
    }
}