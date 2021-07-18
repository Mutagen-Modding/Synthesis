using System;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public interface IToSolutionRunner
    {
        PatcherRunVm GetRunner(PatchersRunVm parent, SolutionPatcherVm slnPatcher);
        PatcherRunVm GetRunner(PatchersRunVm parent, GitPatcherVm gitPatcher);
    }

    public class ToSolutionRunner : IToSolutionRunner
    {
        private readonly IExtraDataPathProvider _extraDataPathProvider;
        private readonly IPatcherRunnerFactory _runnerFactory;

        public ToSolutionRunner(
            IExtraDataPathProvider extraDataPathProvider,
            IPatcherRunnerFactory runnerFactory)
        {
            _extraDataPathProvider = extraDataPathProvider;
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
                        ProjectSubpath = slnPatcher.SelectedProjectPath.TargetPath,
                        SolutionPath = slnPatcher.SolutionPath.TargetPath
                    }, 
                    extraDataFolder: _extraDataPathProvider.Path));
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
                    }, 
                    extraDataFolder: _extraDataPathProvider.Path));
        }
    }
}