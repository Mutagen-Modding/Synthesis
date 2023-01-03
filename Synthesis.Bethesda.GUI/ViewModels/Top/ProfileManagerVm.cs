using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Args;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

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
        
    [Reactive]
    public ProfileVm? SelectedProfile { get; set; }

    public ProfileManagerVm(
        IProfileFactory profileFactory,
        StartingProfileOverrideProvider startingProfileOverrideProvider)
    {
        _profileFactory = profileFactory;
        _startingProfileOverrideProvider = startingProfileOverrideProvider;

        _displayedObject = this.WhenAnyValue(x => x.SelectedProfile!.DisplayController.SelectedObject)
            .ToGuiProperty(this, nameof(DisplayedObject), default, deferSubscription: true);

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