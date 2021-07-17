using System;
using System.IO;
using Autofac;
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
        private readonly ILifetimeScope _scope;
        private readonly IProfileIdentifier _ident;
        public IExtraDataPathProvider ExtraDataPathProvider { get; }
        public IProvideWorkingDirectory WorkingDirectory { get; }

        public PatcherRunnerFactory(
            ILifetimeScope scope,
            IExtraDataPathProvider extraDataPathProvider,
            IProfileIdentifier ident,
            IProvideWorkingDirectory workingDirectory)
        {
            _scope = scope;
            _ident = ident;
            ExtraDataPathProvider = extraDataPathProvider;
            WorkingDirectory = workingDirectory;
        }

        public IPatcherRun Create(PatcherSettings patcherSettings, string? extraDataFolder)
        {
            return patcherSettings switch
            {
                CliPatcherSettings cli => Cli(cli),
                SolutionPatcherSettings sln => Sln(sln, extraDataFolder),
                GithubPatcherSettings git => Git(git),
                _ => throw new NotImplementedException(),
            };
        }

        private ICliPatcherRun Cli(CliPatcherSettings settings)
        {
            var scope = _scope.BeginLifetimeScope();
            var factory = scope.Resolve<CliPatcherRun.Factory>();
            var ret = factory(
                settings.Nickname,
                settings.PathToExecutable,
                pathToExtra: null);
            ret.AddForDisposal(scope);
            return ret;
        }

        private ISolutionPatcherRun Sln(SolutionPatcherSettings settings, string? extraDataFolder)
        {
            var scope = _scope.BeginLifetimeScope(c =>
            {
                c.RegisterInstance(
                        new PathToProjInjection()
                        {
                            Path = Path.Combine(Path.GetDirectoryName(settings.SolutionPath)!, settings.ProjectSubpath)
                        })
                    .As<IPathToProjProvider>();
            });
            var factory = scope.Resolve<SolutionPatcherRun.Factory>();
            var ret = factory(
                name: settings.Nickname,
                pathToSln: settings.SolutionPath,
                pathToExtraDataBaseFolder: extraDataFolder ?? ExtraDataPathProvider.Path);
            
            ret.AddForDisposal(scope);
            return ret;
        }

        private IGitPatcherRun Git(GithubPatcherSettings settings)
        {
            var scope = _scope.BeginLifetimeScope(c =>
            {
                c.RegisterInstance(settings)
                    .AsImplementedInterfaces();
            });
            var factory = scope.Resolve<GitPatcherRun.Factory>();
            var ret = factory(
                settings: settings,
                localDir: GitPatcherRun.RunnerRepoDirectory(WorkingDirectory, _ident.ID, settings.ID));
            ret.AddForDisposal(scope);
            return ret;
        }
    }
}