using System.Reactive.Linq;
using DynamicData;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

public interface IAvailableProjectsFollower
{
    IObservable<IChangeSet<string>> Process(IObservable<FilePath> solutionPath);
}

public class AvailableProjectsFollower : IAvailableProjectsFollower
{
    private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
    private readonly ISchedulerProvider _schedulerProvider;

    public AvailableProjectsFollower(IAvailableProjectsRetriever availableProjectsRetriever, ISchedulerProvider schedulerProvider)
    {
        _availableProjectsRetriever = availableProjectsRetriever;
        _schedulerProvider = schedulerProvider;
    }

    public IObservable<IChangeSet<string>> Process(IObservable<FilePath> solutionPath)
    {
        return solutionPath
            .ObserveOn(_schedulerProvider.TaskPool)
            .Select(x => _availableProjectsRetriever.Get(x))
            .Select(x => x.AsObservableChangeSet())
            .Switch()
            .RefCount();
    }
}