using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface INugetVersioningFollower
{
    IObservable<GetResponse<NugetsVersioningTarget>> ActiveNugetVersion { get; }
}
    
public interface IGitNugetTargetingVm : INugetVersioningFollower
{
    string ManualMutagenVersion { get; set; }
    string ManualSynthesisVersion { get; set; }
    PatcherNugetVersioningEnum MutagenVersioning { get; set; }
    PatcherNugetVersioningEnum SynthesisVersioning { get; set; }
    ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }
    ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }
}

public class GitNugetTargetingVm : ViewModel, IGitNugetTargetingVm
{
    [Reactive] public string ManualMutagenVersion { get; set; } = string.Empty;

    [Reactive] public string ManualSynthesisVersion { get; set; } = string.Empty;

    [Reactive]
    public PatcherNugetVersioningEnum MutagenVersioning { get; set; } = PatcherNugetVersioningEnum.Profile;

    [Reactive]
    public PatcherNugetVersioningEnum SynthesisVersioning { get; set; } = PatcherNugetVersioningEnum.Profile;

    public ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }

    public ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }

    public IObservable<GetResponse<NugetsVersioningTarget>> ActiveNugetVersion { get; }

    public GitNugetTargetingVm(
        ILogger logger,
        INewestProfileLibraryVersionsVm newest,
        IProfileVersioning versioning)
    {
        UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
            objectSource: newest.WhenAnyValue(x => x.NewestMutagenVersion),
            canExecute: v =>
            {
                return Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    v,
                    (manual, latest) => latest != null && latest != manual);
            },
            execute: v => ManualMutagenVersion = v ?? string.Empty,
            extraCanExecute: this.WhenAnyValue(x => x.MutagenVersioning)
                .Select(vers => vers == PatcherNugetVersioningEnum.Manual),
            disposable: this);
        UpdateSynthesisManualToLatestCommand = NoggogCommand.CreateFromObject(
            objectSource: newest.WhenAnyValue(x => x.NewestSynthesisVersion),
            canExecute: v =>
            {
                return Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    v,
                    (manual, latest) => latest != null && latest != manual);
            },
            execute: v => ManualSynthesisVersion = v ?? string.Empty,
            extraCanExecute: this.WhenAnyValue(x => x.SynthesisVersioning)
                .Select(vers => vers == PatcherNugetVersioningEnum.Manual),
            disposable: this);

        ActiveNugetVersion = Observable.CombineLatest(
                versioning.WhenAnyValue(x => x.ActiveVersioning)
                    .Switch(),
                this.WhenAnyValue(x => x.MutagenVersioning),
                this.WhenAnyValue(x => x.ManualMutagenVersion),
                newest.WhenAnyValue(x => x.NewestMutagenVersion),
                this.WhenAnyValue(x => x.SynthesisVersioning),
                this.WhenAnyValue(x => x.ManualSynthesisVersion),
                newest.WhenAnyValue(x => x.NewestSynthesisVersion),
                (profile, mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                {
                    var sb = new StringBuilder("Switching nuget targets");
                    NugetsToUse mutagen, synthesis;
                    if (mutaVersioning == PatcherNugetVersioningEnum.Profile)
                    {
                        sb.Append($"  Mutagen following profile: {profile.Mutagen}");
                        mutagen = profile.Mutagen;
                    }
                    else
                    {
                        mutagen = new NugetsToUse("Mutagen", mutaVersioning.ToNugetVersioningEnum(), mutaManual,
                            newestMuta);
                        sb.Append($"  {mutagen}");
                    }

                    if (synthVersioning == PatcherNugetVersioningEnum.Profile)
                    {
                        sb.Append($"  Synthesis following profile: {profile.Synthesis}");
                        synthesis = profile.Synthesis;
                    }
                    else
                    {
                        synthesis = new NugetsToUse("Synthesis", synthVersioning.ToNugetVersioningEnum(),
                            synthManual, newestSynth);
                        sb.Append($"  {synthesis}");
                    }

                    logger.Information(sb.ToString());
                    return new ActiveNugetVersioning(
                        Mutagen: mutagen,
                        Synthesis: synthesis);
                })
            .Select(nuget => nuget.TryGetTarget())
            .Replay(1)
            .RefCount();
    }
}