using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using Mutagen.Bethesda.Synthesis.Versioning;
using Serilog;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;

#if !DEBUG
using Noggog.Utility;
using System.Diagnostics;
#endif

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        private readonly ISelectedProfileControllerVm _SelectedProfileController;
        private readonly ISettingsSingleton _SettingsSingleton;
        private readonly IActivePanelControllerVm _ActivePanelControllerVm;
        private readonly IProfileFactory _ProfileFactory;
        public ConfigurationVM Configuration { get; }

        private readonly ObservableAsPropertyHelper<ViewModel?> _ActivePanel;
        public ViewModel? ActivePanel => _ActivePanel.Value;

        public ICommand OpenProfilesPageCommand { get; }

        public IConfirmationPanelControllerVm Confirmation { get; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _Hot;
        public bool Hot => _Hot.Value;

        public string SynthesisVersion { get; }
        public string MutagenVersion { get; }

        private readonly ObservableAsPropertyHelper<string?> _NewestSynthesisVersion;
        public string? NewestSynthesisVersion => _NewestSynthesisVersion.Value;

        private readonly ObservableAsPropertyHelper<string?> _NewestMutagenVersion;
        public string? NewestMutagenVersion => _NewestMutagenVersion.Value;

        private readonly ObservableAsPropertyHelper<ConfirmationActionVM?> _ActiveConfirmation;
        public ConfirmationActionVM? ActiveConfirmation => _ActiveConfirmation.Value;

        private readonly ObservableAsPropertyHelper<bool> _InModal;
        public bool InModal => _InModal.Value;

        public IEnvironmentErrorsVM EnvironmentErrors { get; }

        private readonly ObservableAsPropertyHelper<ProfileVM?> _SelectedProfile;
        public ProfileVM? SelectedProfile => _SelectedProfile.Value;

        public MainVM(
            ConfigurationVM configuration,
            IConfirmationPanelControllerVm confirmationControllerVm,
            IProvideCurrentVersions currentVersions,
            ISelectedProfileControllerVm selectedProfile,
            IEnvironmentErrorsVM environmentErrors,
            ISaveSignal saveSignal,
            IRetrieveSaveSettings save,
            ISettingsSingleton settingsSingleton,
            INewestLibraryVersions newestLibVersions,
            IActivePanelControllerVm activePanelControllerVm,
            IProfileFactory profileFactory,
            ILogger logger)
        {
            logger.Information("Creating MainVM");
            _SelectedProfileController = selectedProfile;
            _SettingsSingleton = settingsSingleton;
            _ActivePanelControllerVm = activePanelControllerVm;
            _ProfileFactory = profileFactory;
            _ActivePanel = activePanelControllerVm.WhenAnyValue(x => x.ActivePanel)
                .ToGuiProperty(this, nameof(ActivePanel), default);
            Configuration = configuration;
            activePanelControllerVm.ActivePanel = Configuration;
            Confirmation = confirmationControllerVm;

            _SelectedProfile = _SelectedProfileController.WhenAnyValue(x => x.SelectedProfile)
                .ToGuiProperty(this, nameof(SelectedProfile), default);

            _Hot = this.WhenAnyValue(x => x.ActivePanel)
                .Select(x =>
                {
                    switch (x)
                    {
                        case ConfigurationVM config:
                            return config.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false);
                        case PatchersRunVM running:
                            return running.WhenAnyValue(x => x.Running);
                        default:
                            break;
                    }
                    return Observable.Return(false);
                })
                .Switch()
                .DistinctUntilChanged()
                .ToGuiProperty(this, nameof(Hot));

            OpenProfilesPageCommand = ReactiveCommand.Create(() =>
            {
                activePanelControllerVm.ActivePanel = new ProfilesDisplayVM(Configuration, ActivePanel);
            },
            canExecute: Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.Configuration.CurrentRun!.Running, fallback: false),
                    this.WhenAnyValue(x => x.ActivePanel)
                        .Select(x => x is ProfilesDisplayVM),
                    (running, isProfile) => !running && !isProfile));

            Task.Run(() => Mutagen.Bethesda.WarmupAll.Init()).FireAndForget();

            SynthesisVersion = currentVersions.SynthesisVersion;
            MutagenVersion = currentVersions.MutagenVersion;
            _NewestMutagenVersion = newestLibVersions.NewestMutagenVersion
                .ToGuiProperty(this, nameof(NewestMutagenVersion), default);
            _NewestSynthesisVersion = newestLibVersions.NewestSynthesisVersion
                .ToGuiProperty(this, nameof(NewestSynthesisVersion), default);

            _ActiveConfirmation = Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.Configuration.SelectedProfile!.SelectedPatcher)
                        .Select(x =>
                        {
                            if (x is not GitPatcherVM gitPatcher) return Observable.Return(default(GitPatcherVM?));
                            return gitPatcher.WhenAnyValue(x => x.PatcherSettings.SettingsOpen)
                                .Select(open => open ? (GitPatcherVM?)gitPatcher : null);
                        })
                        .Switch(),
                    this.WhenAnyValue(x => x.Confirmation.TargetConfirmation),
                    (openPatcher, target) =>
                    {
                        if (target != null) return target;
                        if (openPatcher == null) return default(ConfirmationActionVM?);
                        return new ConfirmationActionVM(
                            "External Patcher Settings Open",
                            $"{openPatcher.Nickname} is open for settings manipulation.",
                            toDo: null);
                    })
                .ToGuiProperty(this, nameof(ActiveConfirmation), default(ConfirmationActionVM?));

            _InModal = this.WhenAnyValue(x => x.ActiveConfirmation)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InModal));
            
            EnvironmentErrors = environmentErrors;

            saveSignal.Saving
                .Subscribe((x) => Save(x.Gui, x.Pipe))
                .DisposeWith(this);
        }

        public void Load()
        {
            Configuration.Load(_SettingsSingleton.Gui, _SettingsSingleton.Pipeline);
        }

        private void Save(SynthesisGuiSettings guiSettings, PipelineSettings _)
        {
            guiSettings.MainRepositoryFolder = _SettingsSingleton.Gui.MainRepositoryFolder;
            guiSettings.OpenIdeAfterCreating = _SettingsSingleton.Gui.OpenIdeAfterCreating;
        }

        public void Init()
        {
            if (Configuration.Profiles.Count == 0)
            {
                _ActivePanelControllerVm.ActivePanel = new NewProfileVM(
                    this.Configuration, 
                    _ProfileFactory,
                    (profile) =>
                    {
                        profile.Nickname = profile.Release.ToDescriptionString();
                        _SelectedProfileController.SelectedProfile = profile;
                        _ActivePanelControllerVm.ActivePanel = Configuration;
                    });
            }
        }
    }
}
