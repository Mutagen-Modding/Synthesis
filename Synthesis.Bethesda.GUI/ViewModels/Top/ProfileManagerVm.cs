using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using Noggog;
using Noggog.Reactive;
using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Args;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public interface ISelectedProfileControllerVm
{
    ProfileVm? SelectedProfile { get; set; }
}
    
public class ProfileManagerVm : ViewModel, ISelectedProfileControllerVm, IModifySavingSettings
{
    private readonly IProfileFactory _profileFactory;
    private readonly StartingProfileOverrideProvider _startingProfileOverrideProvider;

    public SourceCache<ProfileVm, string> Profiles { get; } = new(p => p.ID);

    public ReactiveCommandBase<Unit, Unit> RunPatchers { get; }

    private readonly ObservableAsPropertyHelper<ViewModel?> _displayedObject;
    public ViewModel? DisplayedObject => _displayedObject.Value;

    /// <summary>
    /// True when MO2 mode is enabled but Synthesis is running standalone (not inside MO2).
    /// In this state the app exists to build/prepare patchers, so the run button is replaced
    /// with a build-status display.
    /// </summary>
    private readonly ObservableAsPropertyHelper<bool> _mo2PrepMode;
    public bool Mo2PrepMode => _mo2PrepMode.Value;

    private readonly ObservableAsPropertyHelper<Mo2BuildStatus> _mo2BuildStatus;
    public Mo2BuildStatus Mo2BuildStatus => _mo2BuildStatus.Value;

    /// <summary>Opens the Advanced settings page, where the MO2 mode setting lives.</summary>
    public ICommand OpenMo2SettingsCommand { get; }

    [Reactive]
    public ProfileVm? SelectedProfile { get; set; }

    public ProfileManagerVm(
        IProfileFactory profileFactory,
        StartingProfileOverrideProvider startingProfileOverrideProvider,
        IMo2PrepModeProvider mo2PrepMode,
        Lazy<OpenGlobalSettings> openGlobalSettings,
        ActiveRunVm activeRunVm,
        ISchedulerProvider schedulerProvider)
    {
        _profileFactory = profileFactory;
        _startingProfileOverrideProvider = startingProfileOverrideProvider;
        // Resolved lazily to avoid a DI cycle (OpenGlobalSettings -> GlobalSettingsPaneVm
        // -> ProfilesDisplayVm -> ProfileManagerVm).
        OpenMo2SettingsCommand = ReactiveCommand.Create(() =>
            openGlobalSettings.Value.Open(GlobalSettingsPaneVm.SettingsPages.Advanced));

        _displayedObject = this.WhenAnyValue(x => x.SelectedProfile!.DisplayController.SelectedObject)
            .ToGuiProperty(this, nameof(DisplayedObject), default, schedulerProvider.MainThread, deferSubscription: true);

        _mo2PrepMode = mo2PrepMode.ActiveObservable
            .ToGuiProperty(this, nameof(Mo2PrepMode),
                initialValue: mo2PrepMode.Active,
                scheduler: schedulerProvider.MainThread);

        _mo2BuildStatus = activeRunVm.WhenAnyValue(x => x.CurrentRun)
            .Select(run =>
            {
                if (run == null) return Observable.Return(Mo2BuildStatus.None);
                return Observable.CombineLatest(
                    run.WhenAnyValue(x => x.Running),
                    run.WhenAnyValue(x => x.OverallErrored),
                    (running, errored) => running
                        ? Mo2BuildStatus.Building
                        : errored
                            ? Mo2BuildStatus.Failed
                            : Mo2BuildStatus.Succeeded);
            })
            .Switch()
            .ToGuiProperty(this, nameof(Mo2BuildStatus), Mo2BuildStatus.None, schedulerProvider.MainThread, deferSubscription: true);

        RunPatchers = NoggogCommand.CreateFromObject(
            objectSource: this.WhenAnyValue(x => x.SelectedProfile),
            canExecute: (profileObs) => profileObs.Select(profile => profile.WhenAnyValue(x => x!.State))
                .Switch()
                .Select(err => err.Succeeded),
            execute: (profile) =>
            {
                if (profile == null) return;
                profile.StartRun();
            },
            disposable: this);
    }

    public void Load(ISynthesisGuiSettings settings, IPipelineSettings pipeSettings)
    {
        settings.SelectedProfile = _startingProfileOverrideProvider.Get(settings, pipeSettings);
            
        Profiles.Clear();
        Profiles.AddOrUpdate(pipeSettings.Profiles.Select(p =>
        {
            return _profileFactory.Get(p);
        }));
        if (Profiles.TryGetValue(settings.SelectedProfile, out var profile))
        {
            SelectedProfile = profile;
        }
    }

    public void Save(SynthesisGuiSettings guiSettings, PipelineSettings pipeSettings)
    {
        guiSettings.SelectedProfile = SelectedProfile?.ID ?? string.Empty;
        pipeSettings.Profiles = Profiles.Items.Select(p => p.Save()).ToList<ISynthesisProfileSettings>();
    }

    public override void Dispose()
    {
        base.Dispose();
        Profiles.Items.ForEach(p => p.Dispose());
    }
}

public enum Mo2BuildStatus
{
    /// <summary>No run has occurred yet.</summary>
    None,
    /// <summary>A build/run is currently in progress.</summary>
    Building,
    /// <summary>The build completed and patchers are ready to run inside MO2.</summary>
    Succeeded,
    /// <summary>The build encountered an error.</summary>
    Failed,
}