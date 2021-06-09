using System;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IPatcherFactory
    {
        TPatcher Get<TPatcher>()
            where TPatcher : PatcherVM;

        PatcherVM Get(PatcherSettings settings);
    }

    public class PatcherFactory : IPatcherFactory
    {
        private readonly IContainerTracker _Tracker;

        public PatcherFactory(IContainerTracker tracker)
        {
            _Tracker = tracker;
        }
        
        public TPatcher Get<TPatcher>()
            where TPatcher : PatcherVM
        {
            return _Tracker.Container.GetInstance<TPatcher>();
        }

        public PatcherVM Get(PatcherSettings settings)
        {
            return settings switch
            {
                GithubPatcherSettings git => _Tracker.Container
                    .With(git)
                    .GetInstance<GitPatcherVM>(),
                SolutionPatcherSettings soln => _Tracker.Container
                    .With(soln)
                    .GetInstance<SolutionPatcherVM>(),
                CliPatcherSettings cli => _Tracker.Container
                    .With(cli)
                    .GetInstance<CliPatcherVM>(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}