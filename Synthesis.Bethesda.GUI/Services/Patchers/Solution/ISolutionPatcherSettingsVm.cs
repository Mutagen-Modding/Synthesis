using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution;

public interface ISolutionPatcherSettingsSyncTarget
{
    IObservable<PatcherCustomization> Updated { get; }
    public void Update(PatcherCustomization customization);
}

public abstract class TreeCheckBoxItemVm : ViewModel
{
    private bool? _isChecked;
    public bool? IsChecked
    {
        get => _isChecked;
        set => SetIsChecked(value, true, true);
    }
    
    protected abstract TreeCheckBoxItemVm? Parent { get; }
    
    public abstract IReadOnlyList<TreeCheckBoxItemVm> Children { get; }

    void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
    {
        if (value == _isChecked)
            return;

        _isChecked = value;

        if (updateChildren && _isChecked.HasValue)
            this.Children.ForEach(c => c.IsChecked = value);

        if (updateParent && Parent != null)
            Parent.VerifyCheckState();

        this.RaisePropertyChanged(nameof(IsChecked));
    }
    
    void VerifyCheckState()
    {
        bool? state = null;
        for (int i = 0; i < this.Children.Count; ++i)
        {
            bool? current = this.Children[i].IsChecked;
            if (i == 0)
            {
                state = current;
            }
            else if (state != current)
            {
                state = null;
                break;
            }
        }
        this.SetIsChecked(state, false, true);
    }
}

public class GameCategorySelectionVm : TreeCheckBoxItemVm
{
    public GameCategory Category { get; }
    public string Name => Category.ToString();

    public ObservableCollection<GameReleaseSelectionVm> Releases { get; } = new();

    public GameCategorySelectionVm(GameCategory category)
    {
        Category = category;
        Releases.SetTo(category.GetRelatedReleases().Select(r => new GameReleaseSelectionVm(this, r)));
    }

    protected override TreeCheckBoxItemVm? Parent => null;
    public override IReadOnlyList<TreeCheckBoxItemVm> Children => Releases;
}

public class GameReleaseSelectionVm : TreeCheckBoxItemVm
{
    protected override TreeCheckBoxItemVm? Parent { get; }
    public override IReadOnlyList<TreeCheckBoxItemVm> Children => Array.Empty<TreeCheckBoxItemVm>();

    public GameRelease Release { get; }
    public string Name => Release.ToString();

    public GameReleaseSelectionVm(GameCategorySelectionVm? categoryVm, GameRelease release)
    {
        Release = release;
        Parent = categoryVm;
    }
}

public class SolutionPatcherSettingsVm : ViewModel, ISolutionPatcherSettingsSyncTarget
{
    [Reactive]
    public string ShortDescription { get; set; } = string.Empty;

    [Reactive]
    public string LongDescription { get; set; } = string.Empty;

    [Reactive]
    public VisibilityOptions Visibility { get; set; } = DTO.VisibilityOptions.Visible;

    [Reactive]
    public PreferredAutoVersioning Versioning { get; set; }

    public ObservableCollection<ModKey> RequiredMods { get; } = new();
    
    public ObservableCollection<TreeCheckBoxItemVm> TargetedGames { get; }

    public IObservable<PatcherCustomization> Updated { get; }

    private readonly Dictionary<GameRelease, GameReleaseSelectionVm> _releases;

    public SolutionPatcherSettingsVm()
    {
        TargetedGames = new(Enum.GetValues<GameCategory>()
            .Select<GameCategory, TreeCheckBoxItemVm>(x =>
            {
                var rels = x.GetRelatedReleases();
                if (rels.Count() == 1)
                {
                    return new GameReleaseSelectionVm(null, rels.First());
                }
                else
                {
                    return new GameCategorySelectionVm(x);
                }
            }));
        _releases = TargetedGames
            .SelectMany(x =>
            {
                switch (x)
                {
                    case GameCategorySelectionVm category:
                        return category.Releases;
                    case GameReleaseSelectionVm rel:
                        return rel.AsEnumerable();
                    default:
                        throw new NotImplementedException();
                }
            })
            .ToDictionary(x => x.Release, x => x);
        
        Updated = Observable.CombineLatest(
            this.WhenAnyValue(x => x.ShortDescription),
            this.WhenAnyValue(x => x.LongDescription),
            this.WhenAnyValue(x => x.Visibility),
            this.WhenAnyValue(x => x.Versioning),
            RequiredMods.ToObservableChangeSet()
                .QueryWhenChanged()
                .StartWith(Enumerable.Empty<ModKey>()),
            TargetedGames.ToObservableChangeSet()
                .TransformMany(x =>
                {
                    switch (x)
                    {
                        case GameCategorySelectionVm category:
                            return category.Releases;
                        case GameReleaseSelectionVm rel:
                            return rel.AsEnumerable();
                        default:
                            throw new NotImplementedException();
                    }
                })
                .FilterOnObservable(x => x.WhenAnyValue(x => x.IsChecked).Select(x => x ?? true))
                .QueryWhenChanged()
                .Select(x => x.Select(x => x.Release))
                .StartWith(Enumerable.Empty<GameRelease>()),
            (shortDesc, desc, visibility, versioning, reqMods, targetedGames) =>
            {
                return new PatcherCustomization()
                {
                    OneLineDescription = shortDesc,
                    LongDescription = desc,
                    Visibility = visibility,
                    PreferredAutoVersioning = versioning,
                    RequiredMods = reqMods
                        .OrderBy(x => x, ModKey.Alphabetical)
                        .Select(x => x.FileName.String)
                        .ToArray(),
                    TargetedReleases = targetedGames
                        .OrderBy(x => x)
                        .ToArray(),
                };
            });
    }
    
    public void Update(PatcherCustomization customization)
    {
        LongDescription = customization.LongDescription ?? string.Empty;
        ShortDescription = customization.OneLineDescription ?? string.Empty;
        Visibility = customization.Visibility;
        Versioning = customization.PreferredAutoVersioning;
        RequiredMods.SetTo(customization.RequiredMods
            .SelectWhere(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey))
            .OrderBy(x => x, ModKey.Alphabetical));
        TargetedGames.ForEach(x => x.IsChecked = false);
        foreach (var rel in customization.TargetedReleases)
        {
            _releases[rel].IsChecked = true;
        }
    }
}