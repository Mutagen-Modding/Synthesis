using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution;

public interface ISolutionPatcherSettingsVm
{
    string LongDescription { get; set; }
    string ShortDescription { get; set; }
    VisibilityOptions Visibility { get; set; }
    PreferredAutoVersioning Versioning { get; set; }
    ObservableCollection<ModKey> RequiredMods { get; }
    
}

public interface ISolutionPatcherSettingsSyncTarget
{
    IObservable<PatcherCustomization> Updated { get; }
    public void Update(PatcherCustomization customization);
}

public class SolutionPatcherSettingsVm : ViewModel, ISolutionPatcherSettingsVm, ISolutionPatcherSettingsSyncTarget
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

    public IObservable<PatcherCustomization> Updated { get; }

    public SolutionPatcherSettingsVm()
    {
        Updated = Observable.CombineLatest(
            this.WhenAnyValue(x => x.ShortDescription),
            this.WhenAnyValue(x => x.LongDescription),
            this.WhenAnyValue(x => x.Visibility),
            this.WhenAnyValue(x => x.Versioning),
            RequiredMods.ToObservableChangeSet()
                .QueryWhenChanged()
                .StartWith(Enumerable.Empty<ModKey>()),
            (shortDesc, desc, visibility, versioning, reqMods) =>
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
                        .ToArray()
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
    }
}