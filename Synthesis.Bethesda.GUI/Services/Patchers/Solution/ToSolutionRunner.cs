using System;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Settings;
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
        private readonly IPatcherRunnerFactory _runnerFactory;

        public ToSolutionRunner(
            IPatcherRunnerFactory runnerFactory)
        {
            _runnerFactory = runnerFactory;
        }
        
        public PatcherRunVm GetRunner(PatchersRunVm parent, SolutionPatcherVm slnPatcher)
        {
            slnPatcher.PatcherSettings.Persist();
            return new PatcherRunVm(
                parent,
                slnPatcher,
                _runnerFactory.Sln(
                    new SolutionPatcherSettings()
                    {
                        Nickname = slnPatcher.DisplayName,
                        On = true,
                        ProjectSubpath = slnPatcher.SelectedProjectInput.Picker.TargetPath,
                        SolutionPath = slnPatcher.SolutionPath.TargetPath
                    }));
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
                _runnerFactory.Sln(
                    new SolutionPatcherSettings()
                    {
                        Nickname = gitPatcher.DisplayName,
                        On = true,
                        ProjectSubpath = gitPatcher.RunnableData.ProjPath,
                        SolutionPath = gitPatcher.RunnableData.SolutionPath
                    }));
        }
    }
}