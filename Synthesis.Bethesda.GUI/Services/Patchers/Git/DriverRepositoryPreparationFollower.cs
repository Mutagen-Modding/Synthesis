using System.Reactive.Linq;
using Noggog;
using Noggog.Reactive;
using Serilog;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IDriverRepositoryPreparationFollower : ISolutionFilePathFollower
{
    IObservable<ConfigurationState<DriverRepoInfo>> DriverInfo { get; }
}

public class DriverRepositoryPreparationFollower : IDriverRepositoryPreparationFollower
{
    private readonly ILogger _logger;
    private readonly IPrepareDriverRespository _prepareDriverRespository;
    private readonly IGetRepoPathValidity _getRepoPathValidity;
    private readonly ISchedulerProvider _schedulerProvider;
        
    public IObservable<ConfigurationState<DriverRepoInfo>> DriverInfo { get; }

    IObservable<FilePath> ISolutionFilePathFollower.Path => DriverInfo
        .Select(x => x.Item?.SolutionPath ?? default);

    public DriverRepositoryPreparationFollower(
        ILogger logger,
        IPrepareDriverRespository prepareDriverRespository,
        IGetRepoPathValidity getRepoPathValidity,
        ISchedulerProvider schedulerProvider)
    {
        _logger = logger;
        _prepareDriverRespository = prepareDriverRespository;
        _getRepoPathValidity = getRepoPathValidity;
        _schedulerProvider = schedulerProvider;
            
        DriverInfo = _getRepoPathValidity.RepoPath
            .Throttle(TimeSpan.FromMilliseconds(100), _schedulerProvider.MainThread)
            .ObserveOn(_schedulerProvider.TaskPool)
            .SelectReplaceWithIntermediate(
                new ConfigurationState<DriverRepoInfo>(default!)
                {
                    IsHaltingError = false,
                    RunnableState = ErrorResponse.Fail("Cloning driver repository"),
                },
                async (path, cancel) =>
                {
                    if (!path.IsHaltingError && path.RunnableState.Failed)
                        return path.BubbleError<DriverRepoInfo>();
                    using var timing = _logger.Time("Cloning driver repository");

                    return _prepareDriverRespository.Prepare(path.ToGetResponse(), cancel);
                })
            .Replay(1)
            .RefCount();
    }
}