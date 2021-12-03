using System;
using System.Reactive.Linq;
using DynamicData;
using Noggog;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public interface IAvailableProjectsFollower
    {
        IObservable<IChangeSet<string>> Process(IObservable<FilePath> solutionPath);
    }

    public class AvailableProjectsFollower : IAvailableProjectsFollower
    {
        private readonly IAvailableProjectsRetriever _availableProjectsRetriever;

        public AvailableProjectsFollower(IAvailableProjectsRetriever availableProjectsRetriever)
        {
            _availableProjectsRetriever = availableProjectsRetriever;
        }
        
        public IObservable<IChangeSet<string>> Process(IObservable<FilePath> solutionPath)
        {
            return solutionPath
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x => _availableProjectsRetriever.Get(x))
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .RefCount();
        }
    }
}