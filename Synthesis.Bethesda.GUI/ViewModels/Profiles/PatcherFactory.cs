using System;
using System.Reactive.Disposables;
using Autofac;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IPatcherFactory
    {
        PatcherVm Get(PatcherSettings settings);
        GitPatcherVm GetGitPatcher();
        SolutionPatcherVm GetSolutionPatcher();
        CliPatcherVm GetCliPatcher();
    }
    
    public class PatcherFactory : IPatcherFactory
    {
        private readonly ILifetimeScope _scope;

        public PatcherFactory(ILifetimeScope scope)
        {
            _scope = scope;
        }
        
        public PatcherVm Get(PatcherSettings settings)
        {
            var patcherScope = _scope.BeginLifetimeScope(Module.PatcherNickname);
            var patcher = patcherScope.Resolve<ScopedPatcherFactory>()
                .Get(settings);
            patcherScope.DisposeWith(patcher);
            return patcher;
        }

        public GitPatcherVm GetGitPatcher()
        {
            var patcherScope = _scope.BeginLifetimeScope(Module.PatcherNickname);
            var patcher = patcherScope.Resolve<ScopedPatcherFactory>()
                .GetGitPatcher();
            patcherScope.DisposeWith(patcher);
            return patcher;
        }

        public SolutionPatcherVm GetSolutionPatcher()
        {
            var patcherScope = _scope.BeginLifetimeScope(Module.PatcherNickname);
            var patcher = patcherScope.Resolve<ScopedPatcherFactory>()
                .GetSolutionPatcher();
            patcherScope.DisposeWith(patcher);
            return patcher;
        }

        public CliPatcherVm GetCliPatcher()
        {
            var patcherScope = _scope.BeginLifetimeScope(Module.PatcherNickname);
            var patcher = patcherScope.Resolve<ScopedPatcherFactory>()
                .GetCliPatcher();
            patcherScope.DisposeWith(patcher);
            return patcher;
        }
    }
}