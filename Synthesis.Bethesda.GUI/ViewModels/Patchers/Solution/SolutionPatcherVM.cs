using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.WPF.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Profiles.Plugins;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public class SolutionPatcherVM : PatcherVM, IProvidePatcherMetaPath, ISolutionPatcherSettingsVm
    {
        private readonly IProfileLoadOrder _LoadOrder;
        private readonly IToSolutionRunner _ToSolutionRunner;

        public PathPickerVM SolutionPath { get; } = new()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        public IObservableCollection<string> AvailableProjects { get; }

        [Reactive]
        public string ProjectSubpath { get; set; } = string.Empty;

        public PathPickerVM SelectedProjectPath { get; } = new()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State?.Value ?? ConfigurationState.Success;

        public ICommand OpenSolutionCommand { get; }

        [Reactive]
        public string ShortDescription { get; set; } = string.Empty;

        [Reactive]
        public string LongDescription { get; set; } = string.Empty;

        [Reactive]
        public VisibilityOptions Visibility { get; set; } = DTO.VisibilityOptions.Visible;

        [Reactive]
        public PreferredAutoVersioning Versioning { get; set; }

        public ObservableCollectionExtended<PreferredAutoVersioning> VersioningOptions { get; } = new(EnumExt.GetValues<PreferredAutoVersioning>());

        public ObservableCollectionExtended<VisibilityOptions> VisibilityOptions { get; } = new(EnumExt.GetValues<VisibilityOptions>());

        public ObservableCollection<ModKeyItemViewModel> RequiredMods { get; } = new();
         
        public IObservable<IChangeSet<ModKey>> DetectedMods => _LoadOrder.LoadOrder.Connect().Transform(l => l.ModKey);

        public PatcherSettingsVM PatcherSettings { get; }

        public ReactiveCommand<Unit, Unit> ReloadAutogeneratedSettingsCommand { get; }
        
        public IObservable<string> MetaPath { get; }
        IObservable<string> IProvidePatcherMetaPath.Path => MetaPath;

        IObservable<IChangeSet<ModKey>> ISolutionPatcherSettingsVm.RequiredMods => RequiredMods
            .AsObservableChangeSet()
            .Transform(x => x.ModKey);

        public SolutionPatcherVM(
            IProfileLoadOrder loadOrder,
            IRemovePatcherFromProfile remove,
            IInstalledSdkProvider dotNetSdkProviderInstalled,
            IProfileDisplayControllerVm profileDisplay,
            IConfirmationPanelControllerVm confirmation, 
            ILogger logger,
            IPatcherSettingsVmFactory settingsVmFactory,
            IAvailableProjects availableProjects,
            ISolutionProjectPath projectPath,
            ISolutionMetaFileSync metaFileSync,
            IToSolutionRunner toSolutionRunner,
            SolutionPatcherSettings? settings = null)
            : base(remove, profileDisplay, confirmation, settings)
        {
            _LoadOrder = loadOrder;
            _ToSolutionRunner = toSolutionRunner;
            CopyInSettings(settings);
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
            SelectedProjectPath.Filters.Add(new CommonFileDialogFilter("Project", ".csproj"));

            _DisplayName = Observable.CombineLatest(
                this.WhenAnyValue(x => x.Nickname),
                this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath)
                    .StartWith(settings?.ProjectSubpath ?? string.Empty),
                (nickname, path) =>
                {
                    if (!string.IsNullOrWhiteSpace(nickname)) return nickname;
                    try
                    {
                        var name = Path.GetFileName(Path.GetDirectoryName(path));
                        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
                        return name;
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .ToProperty(this, nameof(DisplayName), Nickname);

            AvailableProjects = availableProjects.Process(
                this.WhenAnyValue(x => x.SolutionPath.TargetPath))
                .ObserveOnGui()
                .ToObservableCollection(this);

            var projPath = projectPath.Process(
                solutionPath: this.WhenAnyValue(x => x.SolutionPath.TargetPath),
                projectSubpath: this.WhenAnyValue(x => x.ProjectSubpath));
            projPath
                .Subscribe(p => SelectedProjectPath.TargetPath = p)
                .DisposeWith(this);

            _State = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SolutionPath.ErrorState),
                    this.WhenAnyValue(x => x.SelectedProjectPath.ErrorState),
                    dotNetSdkProviderInstalled.DotNetSdkInstalled,
                    (sln, proj, dotnet) =>
                    {
                        if (sln.Failed) return new ConfigurationState(sln);
                        if (!dotnet.Acceptable) return new ConfigurationState(ErrorResponse.Fail("No dotnet SDK installed"));
                        return new ConfigurationState(proj);
                    })
                .ToGuiProperty<ConfigurationState>(this, nameof(State), new ConfigurationState(ErrorResponse.Fail("Evaluating"))
                {
                    IsHaltingError = false
                });

            OpenSolutionCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.SolutionPath.InError)
                    .Select(x => !x),
                execute: () =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(SolutionPath.TargetPath)
                        {
                            UseShellExecute = true,
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, $"Error opening solution: {SolutionPath.TargetPath}");
                    }
                });

            MetaPath = this.WhenAnyValue(x => x.SelectedProjectPath.TargetPath)
                .Select(projPath =>
                {
                    try
                    {
                        return Path.Combine(Path.GetDirectoryName(projPath)!, Constants.MetaFileName);
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .Replay(1)
                .RefCount();

            metaFileSync.Sync()
                .DisposeWith(this);

            ReloadAutogeneratedSettingsCommand = ReactiveCommand.Create(() => { });
            PatcherSettings = settingsVmFactory.Create(this, Logger, true, 
                projPath
                    .Merge(ReloadAutogeneratedSettingsCommand.EndingExecution()
                        .WithLatestFrom(projPath, (_, p) => p))
                    .Select(p => (GetResponse<FilePath>.Succeed(p), default(string?))))
                .DisposeWith(this);
        }

        public override PatcherSettings Save()
        {
            var ret = new SolutionPatcherSettings();
            CopyOverSave(ret);
            ret.SolutionPath = this.SolutionPath.TargetPath;
            ret.ProjectSubpath = this.ProjectSubpath;
            PatcherSettings.Persist();
            return ret;
        }

        private void CopyInSettings(SolutionPatcherSettings? settings)
        {
            if (settings == null) return;
            this.SolutionPath.TargetPath = settings.SolutionPath;
            this.ProjectSubpath = settings.ProjectSubpath;
        }

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            return _ToSolutionRunner.GetRunner(parent, this);
        }

        public void SetRequiredMods(IEnumerable<ModKey> modKeys)
        {
            RequiredMods.SetTo(modKeys
                .Select(m => new ModKeyItemViewModel(m)));
        }
    }
}
