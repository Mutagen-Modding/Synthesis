using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Versioning;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

#if !DEBUG
using Noggog.Utility;
using System.Diagnostics;
#endif

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public class MainVm : ViewModel, IModifySavingSettings
    {
        private readonly ISelectedProfileControllerVm _selectedProfileController;
        private readonly ISettingsSingleton _settingsSingleton;
        private readonly IActivePanelControllerVm _activePanelControllerVm;
        private readonly IProfileFactory _profileFactory;
        private readonly ILogger _logger;
        public ProfileManagerVm ProfileManager { get; }

        private readonly ObservableAsPropertyHelper<ViewModel?> _activePanel;
        public ViewModel? ActivePanel => _activePanel.Value;

        public ICommand OpenProfilesPageCommand { get; }

        public IConfirmationPanelControllerVm Confirmation { get; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _hot;
        public bool Hot => _hot.Value;

        public string SynthesisVersion { get; }
        public string MutagenVersion { get; }

        private readonly ObservableAsPropertyHelper<string?> _newestSynthesisVersion;
        public string? NewestSynthesisVersion => _newestSynthesisVersion.Value;

        private readonly ObservableAsPropertyHelper<string?> _newestMutagenVersion;
        public string? NewestMutagenVersion => _newestMutagenVersion.Value;

        private readonly ObservableAsPropertyHelper<IConfirmationActionVm?> _activeConfirmation;
        public IConfirmationActionVm? ActiveConfirmation => _activeConfirmation.Value;

        private readonly ObservableAsPropertyHelper<bool> _inModal;
        public bool InModal => _inModal.Value;

        private readonly ObservableAsPropertyHelper<ProfileVm?> _selectedProfile;
        public ProfileVm? SelectedProfile => _selectedProfile.Value;

        [Reactive]
        public bool InitialLoading { get; set; } = true;
        
        public ICommand OpenGlobalSettingsCommand { get; }

        public MainVm(
            ActiveRunVm activeRunVm,
            ProfileManagerVm profileManager,
            OpenProfileSettings openProfileSettings,
            OpenGlobalSettings openGlobalSettings,
            IConfirmationPanelControllerVm confirmationControllerVm,
            IProvideCurrentVersions currentVersions,
            ISelectedProfileControllerVm selectedProfile,
            ISettingsSingleton settingsSingleton,
            INewestLibraryVersionsVm libVersionsVm,
            IActivePanelControllerVm activePanelControllerVm,
            IProfileFactory profileFactory,
            ILogger logger)
        {
            _selectedProfileController = selectedProfile;
            _settingsSingleton = settingsSingleton;
            _activePanelControllerVm = activePanelControllerVm;
            _profileFactory = profileFactory;
            _logger = logger;
            _activePanel = activePanelControllerVm.WhenAnyValue(x => x.ActivePanel!.ViewModel)
                .ToGuiProperty(this, nameof(ActivePanel), default, deferSubscription: true);
            ProfileManager = profileManager;
            activePanelControllerVm.ActivePanel = ProfileManager;
            Confirmation = confirmationControllerVm;

            _selectedProfile = _selectedProfileController.WhenAnyValue(x => x.SelectedProfile)
                .ToGuiProperty(this, nameof(SelectedProfile), default, deferSubscription: true);

            OpenGlobalSettingsCommand = openGlobalSettings.OpenCommand;
            
            _hot = this.WhenAnyValue(x => x.ActivePanel)
                .Select(x =>
                {
                    switch (x)
                    {
                        case ProfileManagerVm config:
                            return activeRunVm.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false);
                        case RunVm running:
                            return running.WhenAnyValue(x => x.Running);
                        default:
                            break;
                    }
                    return Observable.Return(false);
                })
                .Switch()
                .DistinctUntilChanged()
                .ToGuiProperty(this, nameof(Hot), deferSubscription: true);

            OpenProfilesPageCommand = openProfileSettings.OpenCommand;
            
            canExecute: Observable.CombineLatest(
                    activeRunVm.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false),
                    this.WhenAnyValue(x => x.ActivePanel)
                        .Select(x => x is ProfilesDisplayVm),
                    (running, isProfile) => !running && !isProfile);

            Task.Run(() => Mutagen.Bethesda.WarmupAll.Init()).FireAndForget();

            SynthesisVersion = currentVersions.SynthesisVersion;
            MutagenVersion = currentVersions.MutagenVersion;
            _newestMutagenVersion = libVersionsVm.WhenAnyValue(x => x.Versions)
                .Select(x => x.Normal.Mutagen)
                .ToGuiProperty(this, nameof(NewestMutagenVersion), default, deferSubscription: true);
            _newestSynthesisVersion = libVersionsVm.WhenAnyValue(x => x.Versions)
                .Select(x => x.Normal.Synthesis)
                .ToGuiProperty(this, nameof(NewestSynthesisVersion), default, deferSubscription: true);

            _activeConfirmation = Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.ProfileManager.SelectedProfile!.SelectedPatcher)
                        .Select(x =>
                        {
                            if (x is not GitPatcherVm gitPatcher) return Observable.Return(default(GitPatcherVm?));
                            return gitPatcher.WhenAnyValue(x => x.PatcherSettings.SettingsOpen)
                                .Select(open => open ? (GitPatcherVm?)gitPatcher : null);
                        })
                        .Switch(),
                    this.WhenAnyValue(x => x.Confirmation.TargetConfirmation),
                    (openPatcher, target) =>
                    {
                        if (target != null) return target;
                        if (openPatcher == null) return default(IConfirmationActionVm?);
                        return new ConfirmationActionVm(
                            "External Patcher Settings Open",
                            $"{openPatcher.NameVm.Name} is open for settings manipulation.",
                            toDo: null);
                    })
                .ToGuiProperty(this, nameof(ActiveConfirmation), default(ConfirmationActionVm?), deferSubscription: true);

            _inModal = this.WhenAnyValue(x => x.ActiveConfirmation)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InModal), deferSubscription: true);
        }

        public async Task Load()
        {
            _logger.Information("Applying settings");
            ProfileManager.Load(_settingsSingleton.Gui, _settingsSingleton.Pipeline);
            _logger.Information("Settings applied");
        }

        public void Save(SynthesisGuiSettings guiSettings, PipelineSettings _)
        {
            guiSettings.MainRepositoryFolder = _settingsSingleton.Gui.MainRepositoryFolder;
            guiSettings.OpenIdeAfterCreating = _settingsSingleton.Gui.OpenIdeAfterCreating;
        }

        public void Init()
        {
            if (ProfileManager.Profiles.Count == 0)
            {
                _activePanelControllerVm.ActivePanel = new NewProfileVm(
                    this.ProfileManager, 
                    _profileFactory,
                    (profile) =>
                    {
                        _selectedProfileController.SelectedProfile = profile;
                        _activePanelControllerVm.ActivePanel = ProfileManager;
                    });
            }
            InitialLoading = false;
        }
    }
}
