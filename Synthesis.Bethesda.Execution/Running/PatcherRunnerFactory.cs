using System;
using System.IO;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IPatcherRunnerFactory
    {
        IPatcherRun Create(PatcherSettings patcherSettings, string? extraDataFolder);
    }

    public class PatcherRunnerFactory : IPatcherRunnerFactory
    {
        private readonly IProfileIdentifier _ident;
        private readonly CliPatcherRun.Factory _cliFactory;
        private readonly GitPatcherRun.Factory _gitFactory;
        private readonly SolutionPatcherRun.Factory _slnFactory;
        public IWorkingDirectorySubPaths Paths { get; }
        public IProvideWorkingDirectory WorkingDirectory { get; }

        public PatcherRunnerFactory(
            IWorkingDirectorySubPaths paths,
            IProfileIdentifier ident,
            IProvideWorkingDirectory workingDirectory,
            CliPatcherRun.Factory cliFactory,
            GitPatcherRun.Factory gitFactory,
            SolutionPatcherRun.Factory slnFactory)
        {
            _ident = ident;
            _cliFactory = cliFactory;
            _gitFactory = gitFactory;
            _slnFactory = slnFactory;
            Paths = paths;
            WorkingDirectory = workingDirectory;
        }

        public IPatcherRun Create(PatcherSettings patcherSettings, string? extraDataFolder)
        {
            return patcherSettings switch
            {
                CliPatcherSettings cli => _cliFactory(
                    cli.Nickname,
                    cli.PathToExecutable,
                    pathToExtra: null),
                SolutionPatcherSettings sln => _slnFactory(
                    name: sln.Nickname,
                    pathToSln: sln.SolutionPath,
                    pathToExtraDataBaseFolder: extraDataFolder ?? Paths.TypicalExtraData,
                    pathToProj: Path.Combine(Path.GetDirectoryName(sln.SolutionPath)!, sln.ProjectSubpath)),
                GithubPatcherSettings git => _gitFactory(
                    settings: git,
                    localDir: GitPatcherRun.RunnerRepoDirectory(WorkingDirectory, _ident.ID, git.ID)),
                _ => throw new NotImplementedException(),
            };
        }
    }
}