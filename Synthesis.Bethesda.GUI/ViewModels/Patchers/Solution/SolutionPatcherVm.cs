using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.DotNet.Singleton;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

public class SolutionPatcherVm : PatcherVm
{
    public ISolutionPathInputVm SolutionPathInput { get; }
    public ISelectedProjectInputVm SelectedProjectInput { get; }
    private readonly IProfileLoadOrder _loadOrder;
    private readonly ILogger _logger;

    public IObservableCollection<string> AvailableProjects { get; }

    private readonly ObservableAsPropertyHelper<ConfigurationState> _state;
    public override ConfigurationState State => _state?.Value ?? ConfigurationState.Success;

    public ICommand OpenSolutionCommand { get; }

    public ObservableCollectionExtended<PreferredAutoVersioning> VersioningOptions { get; } = new(Enums<PreferredAutoVersioning>.Values);

    public ObservableCollectionExtended<VisibilityOptions> VisibilityOptions { get; } = new(Enums<VisibilityOptions>.Values);
         
    public IObservable<IChangeSet<ModKey>> DetectedMods => _loadOrder.LoadOrder.Connect().Transform(l => l.ModKey);

    public PatcherUserSettingsVm PatcherSettings { get; }

    public ReactiveCommand<Unit, Unit> ReloadAutogeneratedSettingsCommand { get; }
    
    public SolutionPatcherSettingsVm Settings { get; }

    public SolutionPatcherVm(
        ILifetimeScope scope,
        IPatcherNameVm nameVm,
        IProfileLoadOrder loadOrder,
        IInstalledSdkFollower dotNetSdkFollowerInstalled,
        IProfileDisplayControllerVm profileDisplay,
        IConfirmationPanelControllerVm confirmation, 
        ISolutionPathInputVm solutionPathInput,
        ISelectedProjectInputVm selectedProjectInput,
        PatcherUserSettingsVm.Factory settingsVmFactory,
        IAvailableProjectsFollower availableProjectsFollower,
        ISolutionMetaFileSync metaFileSync,
        INavigateTo navigateTo,
        IPatcherIdProvider idProvider,
        ILogger logger,
        SolutionPatcherSettingsVm settingsVm,
        PatcherRenameActionVm.Factory renameFactory,
        PatcherGroupTarget groupTarget,
        SolutionPatcherSettings? settings = null)
        : base(scope, nameVm, profileDisplay, confirmation, idProvider, renameFactory, groupTarget, settings)
    {
        SolutionPathInput = solutionPathInput;
        SelectedProjectInput = selectedProjectInput;
        Settings = settingsVm;
        _loadOrder = loadOrder;
        _logger = logger;
        CopyInSettings(settings);

        AvailableProjects = availableProjectsFollower.Process(
                this.WhenAnyValue(x => x.SolutionPathInput.Picker.TargetPath).Select(x => new FilePath(x)))
            .ObserveOnGui()
            .ToObservableCollection(this);

        _state = Observable.CombineLatest(
                this.WhenAnyValue(x => x.SolutionPathInput.Picker.ErrorState),
                SelectedProjectInput.WhenAnyValue(x => x.Picker.ErrorState),
                dotNetSdkFollowerInstalled.DotNetSdkInstalled,
                (sln, proj, dotnet) =>
                {
                    if (sln.Failed) return new ConfigurationState(sln);
                    if (!dotnet.Acceptable) return new ConfigurationState(ErrorResponse.Fail("No dotnet SDK installed"));
                    return new ConfigurationState(proj);
                })
            .ToGuiProperty<ConfigurationState>(this, nameof(State), new ConfigurationState(ErrorResponse.Fail("Evaluating"))
            {
                IsHaltingError = false
            }, deferSubscription: true);

        OpenSolutionCommand = ReactiveCommand.Create(
            canExecute: this.WhenAnyValue(x => x.SolutionPathInput.Picker.InError)
                .Select(x => !x),
            execute: () =>
            {
                navigateTo.Navigate(SolutionPathInput.Picker.TargetPath);
            });

        metaFileSync.Sync()
            .DisposeWith(this);

        ReloadAutogeneratedSettingsCommand = ReactiveCommand.Create(() => { });
        PatcherSettings = settingsVmFactory(true,
                Observable.CombineLatest(
                        this.WhenAnyValue(x => x.SolutionPathInput.Picker.TargetPath),
                        SelectedProjectInput.Picker.WhenAnyValue(x => x.TargetPath),
                        SelectedProjectInput.WhenAnyValue(x => x.ProjectSubpath),
                        (sln, absProj, relProj) => 
                            new PatcherUserSettingsVm.Inputs(GetResponse<TargetProject>.Succeed(
                                new TargetProject(sln, absProj, relProj)),
                                default(string?), 
                                default))
                    .CombineLatest(
                        ReloadAutogeneratedSettingsCommand
                            .EndingExecution()
                            .StartWith(Unit.Default), 
                        (x, _) => x))
            .DisposeWith(this);
    }

    public override PatcherSettings Save()
    {
        var ret = new SolutionPatcherSettings();
        CopyOverSave(ret);
        ret.SolutionPath = this.SolutionPathInput.Picker.TargetPath;
        ret.ProjectSubpath = this.SelectedProjectInput.ProjectSubpath;
        try
        {
            PatcherSettings.Persist();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save patcher settings");
        }
        return ret;
    }

    private void CopyInSettings(SolutionPatcherSettings? settings)
    {
        if (settings == null) return;
        this.SolutionPathInput.Picker.TargetPath = settings.SolutionPath;
        this.SelectedProjectInput.ProjectSubpath = settings.ProjectSubpath;
    }

    public override void PrepForRun()
    {
        base.PrepForRun();
        try
        {
            PatcherSettings.Persist();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save patcher settings");
        }
    }
}