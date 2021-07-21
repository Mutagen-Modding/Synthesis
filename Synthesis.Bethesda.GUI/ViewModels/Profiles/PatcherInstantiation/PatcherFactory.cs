using System;
using System.Reactive.Disposables;
using Autofac;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation
{
    public interface IPatcherFactory
    {
        PatcherVm Get(PatcherSettings settings);
        GitPatcherVm GetGitPatcher(GithubPatcherSettings? settings = null);
        SolutionPatcherVm GetSolutionPatcher(SolutionPatcherSettings? settings = null);
        CliPatcherVm GetCliPatcher(CliPatcherSettings? settings = null);
    }
    
    public class PatcherFactory : IPatcherFactory
    {
        private readonly ILifetimeScope _scope;
        private readonly IGitSettingsInitializer _gitSettingsInitializer;

        public PatcherFactory(
            ILifetimeScope scope,
            IGitSettingsInitializer gitSettingsInitializer)
        {
            _scope = scope;
            _gitSettingsInitializer = gitSettingsInitializer;
        }
        
        public PatcherVm Get(PatcherSettings settings)
        {
            return settings switch
            {
                GithubPatcherSettings git => GetGitPatcher(git),
                SolutionPatcherSettings soln => GetSolutionPatcher(soln),
                CliPatcherSettings cli => GetCliPatcher(cli),
                _ => throw new NotImplementedException(),
            };
        }

        public GitPatcherVm GetGitPatcher(GithubPatcherSettings? settings = null)
        {
            var patcherScope = _scope.BeginLifetimeScope(MainModule.PatcherNickname, c =>
            {
                c.RegisterModule<GitPatcherModule>();
                settings = _gitSettingsInitializer.Get(settings);
                c.RegisterInstance(settings)
                    .AsSelf()
                    .AsImplementedInterfaces();
            });
            var patcher = patcherScope.Resolve<GitPatcherVm>();
            patcherScope.DisposeWith(patcher);
            return patcher;
        }

        public SolutionPatcherVm GetSolutionPatcher(SolutionPatcherSettings? settings = null)
        {
            var patcherScope = _scope.BeginLifetimeScope(MainModule.PatcherNickname, c =>
            {
                c.RegisterModule<SolutionPatcherModule>();
                if (settings != null)
                {
                    c.RegisterInstance(settings)
                        .AsSelf()
                        .AsImplementedInterfaces();
                }
            });
            var patcher = patcherScope.Resolve<SolutionPatcherVm>();
            patcherScope.DisposeWith(patcher);
            return patcher;
        }

        public CliPatcherVm GetCliPatcher(CliPatcherSettings? settings = null)
        {
            var patcherScope = _scope.BeginLifetimeScope(MainModule.PatcherNickname, c =>
            {
                c.RegisterModule<CliPatcherModule>();
                if (settings != null)
                {
                    c.RegisterInstance(settings)
                        .AsSelf()
                        .AsImplementedInterfaces();
                }
            });
            var patcher = patcherScope.Resolve<CliPatcherVm>();
            patcherScope.DisposeWith(patcher);
            return patcher;
        }
    }
}