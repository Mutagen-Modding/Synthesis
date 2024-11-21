using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Profiles;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public class NewProfileVm : ViewModel
{
    private readonly ProfileManagerVm _config;

    public ObservableCollectionExtended<GameCategory> CategoryOptions { get; } = new();

    public IObservableCollection<GameRelease> ReleaseOptions { get; }

    [Reactive]
    public GameCategory? SelectedCategory { get; set; }

    [Reactive]
    public string Nickname { get; set; } = string.Empty;

    [Reactive]
    public GameRelease? SelectedRelease { get; set; }
    
    public delegate NewProfileVm Factory(
        ProfileManagerVm config,
        Action<ProfileVm>? postRun = null);

    public NewProfileVm(
        IProfileFactory profileFactory,
        CreateProfileId createProfileId,
        ProfileManagerVm config, 
        Action<ProfileVm>? postRun = null)
    {
        _config = config;
        CategoryOptions.AddRange(Enums<GameCategory>.Values);

        ReleaseOptions = this.WhenAnyValue(x => x.SelectedCategory)
            .Select(x => x?.GetRelatedReleases().AsObservableChangeSet() ?? Observable.Return(ChangeSet<GameRelease>.Empty))
            .Switch()
            .ObserveOnGui()
            .ToObservableCollection(this);

        this.WhenAnyValue(x => x.SelectedCategory)
            .WhereNotNull()
            .Select(x => x.GetRelatedReleases())
            .Where(x => x.Count() == 1)
            .Subscribe(rel =>
            {
                SelectedRelease = rel.FirstOrDefault();
            })
            .DisposeWith(this);

        this.WhenAnyValue(x => x.SelectedRelease)
            .Subscribe(game =>
            {
                if (game == null) return;
                var existing = config.Profiles.Items.Select(x => x.ID).ToHashSet();
                var profile = profileFactory.Get(game.Value, createProfileId.GetNewProfileId(existing), Nickname.IsNullOrWhitespace() ? game.Value.ToDescriptionString() : Nickname);
                config.Profiles.AddOrUpdate(profile);
                postRun?.Invoke(profile);
            })
            .DisposeWith(this);
    }
}