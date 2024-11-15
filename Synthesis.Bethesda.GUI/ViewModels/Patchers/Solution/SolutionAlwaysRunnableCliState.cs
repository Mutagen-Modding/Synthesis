using System.Reactive.Linq;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.Services.Patchers.Git;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

public class SolutionAlwaysRunnableCliState : IPatcherRunnabilityCliState
{
    public IObservable<ConfigurationState<RunnerRepoInfo>> Runnable => Observable.Empty<ConfigurationState<RunnerRepoInfo>>();
    
    public void CheckAgain()
    {
    }
}