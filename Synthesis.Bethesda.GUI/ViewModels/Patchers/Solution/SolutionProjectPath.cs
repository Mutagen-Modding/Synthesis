using System;
using System.IO;
using System.Reactive.Linq;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public interface ISolutionProjectPath
    {
        IObservable<string> Process(IObservable<string> solutionPath, IObservable<string> projectSubpath);
    }

    public class SolutionProjectPath : ISolutionProjectPath
    {
        public IObservable<string> Process(IObservable<string> solutionPath, IObservable<string> projectSubpath)
        {
            return projectSubpath
                // Need to throttle, as bindings flip to null quickly, which we want to skip
                .Throttle(TimeSpan.FromMilliseconds(150), RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .CombineLatest(solutionPath.DistinctUntilChanged(),
                    (subPath, slnPath) =>
                    {
                        try
                        {
                            return Path.Combine(Path.GetDirectoryName(slnPath)!, subPath);
                        }
                        catch (Exception)
                        {
                            return string.Empty;
                        }
                    })
                .Replay(1)
                .RefCount();
        }
    }
}