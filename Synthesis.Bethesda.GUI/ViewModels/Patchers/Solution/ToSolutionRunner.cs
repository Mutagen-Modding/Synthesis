using System;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Pathing;
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
        private readonly IBuild _Build;
        private readonly IExtraDataPathProvider _extraDataPathProvider;
        private readonly ICheckRunnability _CheckRunnability;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideRepositoryCheckouts _ProvideRepositoryCheckouts;

        public ToSolutionRunner(
            IBuild build,
            IExtraDataPathProvider extraDataPathProvider,
            ICheckRunnability checkRunnability,
            IProcessFactory processFactory,
            IProvideRepositoryCheckouts provideRepositoryCheckouts)
        {
            _Build = build;
            _extraDataPathProvider = extraDataPathProvider;
            _CheckRunnability = checkRunnability;
            _ProcessFactory = processFactory;
            _ProvideRepositoryCheckouts = provideRepositoryCheckouts;
        }
        
        public PatcherRunVm GetRunner(PatchersRunVm parent, SolutionPatcherVm slnPatcher)
        {
            slnPatcher.PatcherSettings.Persist();
            return new PatcherRunVm(
                parent,
                slnPatcher,
                new SolutionPatcherRun(
                    name: slnPatcher.DisplayName,
                    pathToSln: slnPatcher.SolutionPath.TargetPath,
                    pathToExtraDataBaseFolder: _extraDataPathProvider.Path,
                    pathToProjProvider: new PathToProjInjection()
                    {
                        Path = slnPatcher.SelectedProjectPath.TargetPath
                    },
                    build: _Build,
                    checkRunnability: _CheckRunnability,
                    processFactory: _ProcessFactory,
                    repositoryCheckouts: _ProvideRepositoryCheckouts));
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
                new SolutionPatcherRun(
                    name: gitPatcher.DisplayName,
                    pathToSln: gitPatcher.RunnableData.SolutionPath,
                    pathToExtraDataBaseFolder: _extraDataPathProvider.Path,
                    pathToProjProvider: new PathToProjInjection()
                    {
                        Path = gitPatcher.RunnableData.ProjPath
                    },
                    build: _Build,
                    processFactory: _ProcessFactory,
                    checkRunnability: _CheckRunnability,
                    repositoryCheckouts: _ProvideRepositoryCheckouts));
        }
    }
}