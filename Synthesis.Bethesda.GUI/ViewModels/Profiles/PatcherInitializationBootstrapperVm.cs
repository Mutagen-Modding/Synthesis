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
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public interface IPatcherInitializationVm
{
    ICommand AddGitPatcherCommand { get; }
    ICommand AddSolutionPatcherCommand { get; }
    ICommand AddCliPatcherCommand { get; }
    ICommand AddGroupCommand { get; }
    ICommand CompleteConfiguration { get; }
    ICommand CancelConfiguration { get; }
    IPatcherInitVm? NewPatcher { get; set; }
}

public class PatcherInitializationBootstrapperVm : ViewModel, IPatcherInitializationVm
{
    private readonly ILifetimeScope _scope;

    public ICommand AddGitPatcherCommand { get; }
    public ICommand AddSolutionPatcherCommand { get; }
    public ICommand AddCliPatcherCommand { get; }
    public ICommand AddGroupCommand { get; }
    public ICommand CompleteConfiguration { get; }
        
    public ICommand CancelConfiguration { get; }

    [Reactive]
    public IPatcherInitVm? NewPatcher { get; set; }
        
    public PatcherInitializationBootstrapperVm(
        ILifetimeScope scope,
        IProfileGroupsList groupsList,
        INewGroupCreator groupCreator,
        IAddPatchersToSelectedGroupVm addNewPatchersVm,
        IProfileDisplayControllerVm displayControllerVm,
        PatcherInitRenameValidator renamer)
    {
        _scope = scope;

        var hasAnyGroups = groupsList.Groups.CountChanged
            .Select(x => x > 0)
            .Replay(1)
            .RefCount();

        AddGitPatcherCommand = ReactiveCommand.Create(
            () => { NewPatcher = Resolve<GitPatcherInitVm, GuiGitPatcherModule, GithubPatcherSettings>(); },
            canExecute: hasAnyGroups);
        AddSolutionPatcherCommand = ReactiveCommand.Create(
            () => { NewPatcher = Resolve<SolutionPatcherInitVm, GuiSolutionPatcherModule, SolutionPatcherSettings>(); },
            hasAnyGroups);
        AddCliPatcherCommand = ReactiveCommand.Create(
            () => { NewPatcher = Resolve<CliPatcherInitVm, GuiCliPatcherModule, CliPatcherSettings>(); },
            hasAnyGroups);
        AddGroupCommand = ReactiveCommand.Create(() =>
        {
            var group = groupCreator.Get();
            groupsList.Groups.Add(group);
            displayControllerVm.SelectedObject = group;
        });
        CompleteConfiguration = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var initializer = NewPatcher;
                if (initializer == null) return;
                var list = await initializer.Construct().ToArrayAsync().ConfigureAwait(false);
                foreach (var item in list)
                {
                    if (!await renamer.ConfirmNameUnique(item)) return;
                }

                NewPatcher = null;
                if (list.Length == 0) return;
                addNewPatchersVm.AddNewPatchers(list);
                displayControllerVm.SelectedObject = list.First();
            },
            canExecute: this.WhenAnyValue(x => x.NewPatcher)
                .Select(patcher =>
                {
                    if (patcher == null) return Observable.Return(false);
                    return patcher.WhenAnyValue(x => x.CanCompleteConfiguration)
                        .Select(e => e.Succeeded)
                        .CombineLatest(
                            addNewPatchersVm.WhenAnyValue(x => x.CanAddPatchers),
                            (canComplete, canAdd) => canComplete && canAdd);
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

    private TInit Resolve<TInit, TModule, TSettings>()
        where TInit : IPatcherInitVm
        where TModule : Autofac.Module, new()
        where TSettings : class, new()
    {
        var initScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
        {
            c.RegisterInstance(new TSettings())
                .AsSelf()
                .AsImplementedInterfaces();
            c.RegisterModule<TModule>();
        });
        var init = initScope.Resolve<TInit>();
        initScope.DisposeWith(init);
        return init;
    }
}