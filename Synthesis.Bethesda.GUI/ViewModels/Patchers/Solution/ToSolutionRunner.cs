using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public interface IToSolutionRunner
    {
        PatcherRunVM GetRunner(PatchersRunVM parent, SolutionPatcherVM slnPatcher);
    }

    public class ToSolutionRunner : IToSolutionRunner
    {
        private readonly IWorkingDirectorySubPaths _Paths;
        private readonly IBuild _Build;
        private readonly ICheckRunnability _CheckRunnability;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideRepositoryCheckouts _ProvideRepositoryCheckouts;

        public ToSolutionRunner(
            IWorkingDirectorySubPaths paths,
            IBuild build,
            ICheckRunnability checkRunnability,
            IProcessFactory processFactory,
            IProvideRepositoryCheckouts provideRepositoryCheckouts)
        {
            _Paths = paths;
            _Build = build;
            _CheckRunnability = checkRunnability;
            _ProcessFactory = processFactory;
            _ProvideRepositoryCheckouts = provideRepositoryCheckouts;
        }
        
        public PatcherRunVM GetRunner(PatchersRunVM parent, SolutionPatcherVM slnPatcher)
        {
            slnPatcher.PatcherSettings.Persist();
            return new PatcherRunVM(
                parent,
                slnPatcher,
                new SolutionPatcherRun(
                    name: slnPatcher.DisplayName,
                    pathToSln: slnPatcher.SolutionPath.TargetPath,
                    pathToExtraDataBaseFolder: _Paths.TypicalExtraData,
                    pathToProj: slnPatcher.SelectedProjectPath.TargetPath,
                    build: _Build,
                    checkRunnability: _CheckRunnability,
                    processFactory: _ProcessFactory,
                    repositoryCheckouts: _ProvideRepositoryCheckouts));
        }
    }
}