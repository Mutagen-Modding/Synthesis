using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.GUI.Services.Versioning;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public interface INewestProfileLibraryVersionsVm
{
    string? NewestSynthesisVersion { get; }
    string? NewestMutagenVersion { get; }
}

public class NewestProfileLibraryVersionsVm : ViewModel, INewestProfileLibraryVersionsVm
{
    public IConsiderPrereleasePreference ConsiderPrerelease { get; }

    private readonly ObservableAsPropertyHelper<string?> _newestSynthesisVersion;
    public string? NewestSynthesisVersion => _newestSynthesisVersion.Value;

    private readonly ObservableAsPropertyHelper<string?> _newestMutagenVersion;
    public string? NewestMutagenVersion => _newestMutagenVersion.Value;

    public NewestProfileLibraryVersionsVm(
        INewestLibraryVersionsVm newestLibraryVersionsVm,
        IConsiderPrereleasePreference considerPrerelease)
    {
        ConsiderPrerelease = considerPrerelease;
            
        _newestMutagenVersion = Observable.CombineLatest(
                newestLibraryVersionsVm.WhenAnyValue(x => x.Versions),
                considerPrerelease.ConsiderPrereleases,
                (vers, prereleases) => prereleases ? vers.Prerelease.Mutagen : vers.Normal.Mutagen)
            .ToGuiProperty(this, nameof(NewestMutagenVersion), default(string?), deferSubscription: true);
        _newestSynthesisVersion = Observable.CombineLatest(
                newestLibraryVersionsVm.WhenAnyValue(x => x.Versions),
                considerPrerelease.ConsiderPrereleases,
                (vers, prereleases) => prereleases ? vers.Prerelease.Synthesis : vers.Normal.Synthesis)
            .ToGuiProperty(this, nameof(NewestSynthesisVersion), default(string?), deferSubscription: true);
    }
}