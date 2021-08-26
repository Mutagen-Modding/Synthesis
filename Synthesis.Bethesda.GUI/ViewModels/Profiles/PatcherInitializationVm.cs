using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using DynamicData;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IPatcherInitializationVm
    {
        ICommand AddGitPatcherCommand { get; }
        ICommand AddSolutionPatcherCommand { get; }
        ICommand AddCliPatcherCommand { get; }
        ICommand CompleteConfiguration { get; }
        ICommand CancelConfiguration { get; }
        IPatcherInitVm? NewPatcher { get; set; }
        void AddNewPatchers(List<PatcherVm> patchersToAdd);
    }

    public class PatcherInitializationVm : ViewModel, IPatcherInitializationVm
    {
        private readonly ILifetimeScope _scope;
        private readonly IProfilePatchersList _patchersList;
        private readonly IProfileDisplayControllerVm _displayControllerVm;

        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }
        public ICommand CompleteConfiguration { get; }
        
        public ICommand CancelConfiguration { get; }

        [Reactive]
        public IPatcherInitVm? NewPatcher { get; set; }

        public PatcherInitializationVm(
            ILifetimeScope scope,
            IProfilePatchersList patchersList,
            IProfileDisplayControllerVm displayControllerVm)
        {
            _scope = scope;
            _patchersList = patchersList;
            _displayControllerVm = displayControllerVm;
            AddGitPatcherCommand = ReactiveCommand.Create(() =>
            {
                NewPatcher = Resolve<GitPatcherInitVm, GuiGitPatcherModule, GithubPatcherSettings>();
            });
            AddSolutionPatcherCommand = ReactiveCommand.Create(() =>
            {
                NewPatcher = Resolve<SolutionPatcherInitVm, GuiSolutionPatcherModule, SolutionPatcherSettings>();
            });
            AddCliPatcherCommand = ReactiveCommand.Create(() =>
            {
                NewPatcher = Resolve<CliPatcherInitVm, GuiCliPatcherModule, CliPatcherSettings>();
            });
            CompleteConfiguration = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var initializer = this.NewPatcher;
                    if (initializer == null) return;
                    AddNewPatchers(await initializer.Construct().ToListAsync());
                },
                canExecute: this.WhenAnyValue(x => x.NewPatcher)
                    .Select(patcher =>
                    {
                        if (patcher == null) return Observable.Return(false);
                        return patcher.WhenAnyValue(x => x.CanCompleteConfiguration)
                            .Select(e => e.Succeeded);
                    })
                    .Switch());

            CancelConfiguration = ReactiveCommand.Create(
                () =>
                {
                    NewPatcher?.Cancel();
                    NewPatcher = null;
                });

            // Dispose any old patcher initializations
            this.WhenAnyValue(x => x.NewPatcher)
                .DisposePrevious()
                .Subscribe()
                .DisposeWith(this);
        }

        public void AddNewPatchers(List<PatcherVm> patchersToAdd)
        {
            NewPatcher = null;
            if (patchersToAdd.Count == 0) return;
            patchersToAdd.ForEach(p => p.IsOn = true);
            _patchersList.Patchers.AddRange(patchersToAdd);
            _displayControllerVm.SelectedObject = patchersToAdd.First();
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