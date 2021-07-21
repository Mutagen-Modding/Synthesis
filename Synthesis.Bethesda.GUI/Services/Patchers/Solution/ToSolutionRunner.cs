using System;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface IToSolutionRunner
    {
        PatcherRunVm GetRunner(PatchersRunVm parent, SolutionPatcherVm slnPatcher);
        PatcherRunVm GetRunner(PatchersRunVm parent, GitPatcherVm gitPatcher);
    }

    public class ToSolutionRunner : IToSolutionRunner
    {
        private readonly Func<ISolutionPatcherRun> _creator;

        public ToSolutionRunner(
            Func<ISolutionPatcherRun> creator)
        {
            _creator = creator;
        }
        
        public PatcherRunVm GetRunner(PatchersRunVm parent, SolutionPatcherVm slnPatcher)
        {
            slnPatcher.PatcherSettings.Persist();
            return new PatcherRunVm(
                parent,
                slnPatcher,
                _creator());
        }

        public PatcherRunVm GetRunner(PatchersRunVm parent, GitPatcherVm gitPatcher)
        {
            if (gitPatcher.RunnableData == null)
            {
                throw new ArgumentNullException();
            }
            return new PatcherRunVm(
                parent,
                gitPatcher,
                _creator());
        }
    }
}