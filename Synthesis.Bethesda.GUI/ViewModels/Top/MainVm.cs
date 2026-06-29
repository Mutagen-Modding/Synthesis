using System.Reactive.Linq;
using System.Windows.Input;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.Reactive;
using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Settings.V2;
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
        private readonly ILogger _logger;
        private readonly NewProfileVm.Factory _newProfileVmFactory;
        private readonly Mo2PromptVm _mo2PromptVm;
        public ProfileManagerVm ProfileManager { get; }

        private readonly ObservableAsPropertyHelper<ViewModel?> _activePanel;
        public ViewModel? ActivePanel => _activePanel.Value;

        public IConfirmationPanelControllerVm Confirmation { get; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _hot;
        public bool Hot => _hot.Value;

        public string SynthesisVersion { get; }

        private readonly ObservableAsPropertyHelper<IConfirmationActionVm?> _activeConfirmation;
        public IConfirmationActionVm? ActiveConfirmation => _activeConfirmation.Value;

        private readonly ObservableAsPropertyHelper<bool> _inModal;
        public bool InModal => _inModal.Value;

        private readonly ObservableAsPropertyHelper<ProfileVm?> _selectedProfile;
        public ProfileVm? SelectedProfile => _selectedProfile.Value;

        [Reactive]
        public bool InitialLoading { get; set; } = true;
        
        public ICommand OpenGlobalSettingsCommand { get; }
        public ICommand OpenProfilesPageCommand { get; }
        public ICommand OpenUiVersionPageCommand { get; }

        public UiUpdateVm UiUpdateVm { get; }
        
        public MainVm(
            ActiveRunVm activeRunVm,
            ProfileManagerVm profileManager,
            OpenGlobalSettings openGlobalSettings,
            IConfirmationPanelControllerVm confirmationControllerVm,
            IProvideCurrentVersions currentVersions,
            ISelectedProfileControllerVm selectedProfile,
            ISettingsSingleton settingsSingleton,
            IActivePanelControllerVm activePanelControllerVm,
            ILogger logger,
            UiUpdateVm uiUpdateVm,
            NewProfileVm.Factory newProfileVmFactory,
            Mo2PromptVm mo2PromptVm,
            ISchedulerProvider schedulerProvider)
        {
            _selectedProfileController = selectedProfile;
            _settingsSingleton = settingsSingleton;
            _activePanelControllerVm = activePanelControllerVm;
            _logger = logger;
            _newProfileVmFactory = newProfileVmFactory;
            _mo2PromptVm = mo2PromptVm;
            UiUpdateVm = uiUpdateVm;
            _activePanel = activePanelControllerVm.WhenAnyValue(x => x.ActivePanel!.ViewModel)
                .ToGuiProperty(this, nameof(ActivePanel), default, schedulerProvider.MainThread, deferSubscription: true);
            ProfileManager = profileManager;
            activePanelControllerVm.ActivePanel = ProfileManager;
            Confirmation = confirmationControllerVm;

            _selectedProfile = _selectedProfileController.WhenAnyValue(x => x.SelectedProfile)
                .ToGuiProperty(this, nameof(SelectedProfile), default, schedulerProvider.MainThread, deferSubscription: true);

            OpenGlobalSettingsCommand = openGlobalSettings.OpenGlobalSettingsCommand;
            OpenProfilesPageCommand = openGlobalSettings.OpenProfilesPageCommand;
            OpenUiVersionPageCommand = openGlobalSettings.OpenUiVersionPageCommand;
            
            _hot = this.WhenAnyValue(x => x.ActivePanel)
                .Select(x =>
                {
                    switch (x)
                    {
                        case ProfileManagerVm:
                            return activeRunVm.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false);
                        case RunVm running:
                            return running.WhenAnyValue(x => x.Running);
                    }
                    return Observable.Return(false);
                })
                .Switch()
                .DistinctUntilChanged()
                .ToGuiProperty(this, nameof(Hot), schedulerProvider.MainThread, deferSubscription: true);

            Task.Run(Warmup.Init).FireAndForget();

            SynthesisVersion = currentVersions.SynthesisVersion;

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
                            $"{openPatcher.NameVm.Name} is open(ing) for settings manipulation.",
                            toDo: null);
                    })
                .ToGuiProperty(this, nameof(ActiveConfirmation), default(ConfirmationActionVm?), schedulerProvider.MainThread, deferSubscription: true);

            _inModal = this.WhenAnyValue(x => x.ActiveConfirmation)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InModal), schedulerProvider.MainThread, deferSubscription: true);

            _mo2PromptVm.ConfirmCommand
                .Subscribe(_ => _activePanelControllerVm.ActivePanel = ProfileManager)
                .DisposeWith(this);
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
                _activePanelControllerVm.ActivePanel = _newProfileVmFactory(
                    this.ProfileManager,
                    (profile) =>
                    {
                        _selectedProfileController.SelectedProfile = profile;
                        ShowMainOrMo2Prompt();
                    });
            }
            else
            {
                ShowMainOrMo2Prompt();
            }
            InitialLoading = false;
        }

        private void ShowMainOrMo2Prompt()
        {
            if (_mo2PromptVm.ShouldShow)
            {
                _activePanelControllerVm.ActivePanel = _mo2PromptVm;
            }
            else
            {
                _activePanelControllerVm.ActivePanel = ProfileManager;
            }
        }
    }
}
