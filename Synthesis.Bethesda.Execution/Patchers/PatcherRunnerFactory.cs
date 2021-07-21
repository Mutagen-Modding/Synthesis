using System;
using System.IO;
using Autofac;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public interface IPatcherRunnerFactory
    {
        IPatcherRun Create(PatcherSettings patcherSettings);
        ICliPatcherRun Cli(CliPatcherSettings settings);
        ISolutionPatcherRun Sln(SolutionPatcherSettings settings);
        IGitPatcherRun Git(GithubPatcherSettings settings);
    }

    public class PatcherRunnerFactory : IPatcherRunnerFactory
    {
        private readonly ILifetimeScope _scope;
        public IExtraDataPathProvider ExtraDataPathProvider { get; }
        public IProvideWorkingDirectory WorkingDirectory { get; }

        public PatcherRunnerFactory(
            ILifetimeScope scope,
            IExtraDataPathProvider extraDataPathProvider,
            IProvideWorkingDirectory workingDirectory)
        {
            _scope = scope;
            ExtraDataPathProvider = extraDataPathProvider;
            WorkingDirectory = workingDirectory;
        }

        public IPatcherRun Create(PatcherSettings patcherSettings)
        {
            return patcherSettings switch
            {
                CliPatcherSettings cli => Cli(cli),
                SolutionPatcherSettings sln => Sln(sln),
                GithubPatcherSettings git => Git(git),
                _ => throw new NotImplementedException(),
            };
        }

        public ICliPatcherRun Cli(CliPatcherSettings settings)
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

        public ISolutionPatcherRun Sln(SolutionPatcherSettings settings)
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
                pathToSln: settings.SolutionPath);
            
            ret.AddForDisposal(scope);
            return ret;
        }

        public IGitPatcherRun Git(GithubPatcherSettings settings)
        {
            var scope = _scope.BeginLifetimeScope(c =>
            {
                c.RegisterInstance(settings)
                    .AsImplementedInterfaces();
            });
            var factory = scope.Resolve<GitPatcherRun.Factory>();
            var ret = factory(
                settings: settings);
            ret.AddForDisposal(scope);
            return ret;
        }
    }
}