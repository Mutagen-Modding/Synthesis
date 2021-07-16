using System;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public interface IAvailableProjects
    {
        IObservable<IChangeSet<string>> Process(IObservable<string> solutionPath);
    }

    public class AvailableProjects : IAvailableProjects
    {
        public IObservable<IChangeSet<string>> Process(IObservable<string> solutionPath)
        {
            return solutionPath
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(SolutionPatcherRun.AvailableProjects)
                .Select(x => x.AsObservableChangeSet())
                .Switch()
                .RefCount();
        }
    }
}