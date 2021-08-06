using System.Windows.Input;
using Autofac;
using Noggog;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization
{
    public interface IPatcherInitializationFactoryVm
    {
        ICommand AddGitPatcherCommand { get; }
        ICommand AddSolutionPatcherCommand { get; }
        ICommand AddCliPatcherCommand { get; }
    }

    public class PatcherInitializationFactoryVm : IPatcherInitializationFactoryVm
    {
        private readonly ILifetimeScope _scope;
        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }

        public PatcherInitializationFactoryVm(
            ILifetimeScope scope,
            IPatcherInitializationVm initializationVm)
        {
            _scope = scope;
            AddGitPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = Resolve<GitPatcherInitVm, GitPatcherModule, GithubPatcherSettings>();
            });
            AddSolutionPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = Resolve<SolutionPatcherInitVm, SolutionPatcherModule, SolutionPatcherSettings>();
            });
            AddCliPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = Resolve<CliPatcherInitVm, CliPatcherModule, CliPatcherSettings>();
            });
        }

        private TInit Resolve<TInit, TModule, TSettings>()
            where TInit : IPatcherInitVm
            where TModule : Autofac.Module, new()
            where TSettings : class, new()
        {
            var initScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
            {
                c.RegisterInstance(new TSettings());
                c.RegisterModule<TModule>();
            });
            var init = initScope.Resolve<TInit>();
            initScope.DisposeWith(init);
            return init;
        }
    }
}