using System.Windows.Input;
using Autofac;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
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
                initializationVm.NewPatcher = Resolve<GitPatcherInitVm>();
            });
            AddSolutionPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = Resolve<SolutionPatcherInitVm>();
            });
            AddCliPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = Resolve<CliPatcherInitVm>();
            });
        }

        private T Resolve<T>()
            where T : PatcherInitVm
        {
            var initScope = _scope.BeginLifetimeScope(Module.PatcherNickname);
            var init = initScope.Resolve<T>();
            initScope.DisposeWith(init);
            return init;
        }
    }
}