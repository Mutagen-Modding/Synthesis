using System.Reactive;
using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfileVersioning
{
    NugetVersioningEnum MutagenVersioning { get; set; }
    string? ManualMutagenVersion { get; set; }
    NugetVersioningEnum SynthesisVersioning { get; set; }
    string? ManualSynthesisVersion { get; set; }
    IObservable<ActiveNugetVersioning> ActiveVersioning { get; }
    ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }
    ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }
    IReactiveCommand UpdateProfileNugetVersionCommand { get; }
}

public class ProfileVersioning : ViewModel, IProfileVersioning
{
    [Reactive]
    public NugetVersioningEnum MutagenVersioning { get; set; } = NugetVersioningEnum.Manual;

    [Reactive]
    public string? ManualMutagenVersion { get; set; }

    [Reactive]
    public NugetVersioningEnum SynthesisVersioning { get; set; } = NugetVersioningEnum.Manual;

    [Reactive]
    public string? ManualSynthesisVersion { get; set; }
        
    public IObservable<ActiveNugetVersioning> ActiveVersioning { get; }

    public ReactiveCommand<Unit, Unit> UpdateMutagenManualToLatestCommand { get; }

    public ReactiveCommand<Unit, Unit> UpdateSynthesisManualToLatestCommand { get; }
        
    public IReactiveCommand UpdateProfileNugetVersionCommand { get; }

    public ProfileVersioning(
        ILogger logger,
        IProfileNameProvider nameProvider,
        CalculateProfileVersioning calculateProfileVersioning,
        INewestProfileLibraryVersionsVm libs)
    {
        ActiveVersioning = Observable.CombineLatest(
                this.WhenAnyValue(x => x.MutagenVersioning),
                this.WhenAnyValue(x => x.ManualMutagenVersion),
                libs.WhenAnyValue(x => x.NewestMutagenVersion),
                this.WhenAnyValue(x => x.SynthesisVersioning),
                this.WhenAnyValue(x => x.ManualSynthesisVersion),
                libs.WhenAnyValue(x => x.NewestSynthesisVersion),
                (mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                {
                    return calculateProfileVersioning.Calculate(
                        mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth
                    );
                })
            .Do(x => logger.Information("Swapped profile {Nickname} to {Versioning}", nameProvider.Name, x))
            .ObserveOnGui()
            .Replay(1)
            .RefCount();

        // Set manual if empty
        libs.WhenAnyValue(x => x.NewestMutagenVersion)
            .Subscribe(x =>
            {
                if (ManualMutagenVersion == null)
                {
                    ManualMutagenVersion = x;
                }
            })
            .DisposeWith(this);
        libs.WhenAnyValue(x => x.NewestSynthesisVersion)
            .Subscribe(x =>
            {
                if (ManualSynthesisVersion == null)
                {
                    ManualSynthesisVersion = x;
                }
            })
            .DisposeWith(this);

        UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
            objectSource: libs.WhenAnyValue(x => x.NewestMutagenVersion)
                .ObserveOnGui(),
            canExecute: v =>
            {
                return Observable.CombineLatest(
                        this.WhenAnyValue(x => x.MutagenVersioning),
                        this.WhenAnyValue(x => x.ManualMutagenVersion),
                        v,
                        (versioning, manual, latest) =>
                        {
                            if (versioning != NugetVersioningEnum.Manual) return false;
                            return latest != null && latest != manual;
                        })
                    .ObserveOnGui();
            },
            execute: v => ManualMutagenVersion = v ?? string.Empty,
            disposable: this);
        UpdateSynthesisManualToLatestCommand = NoggogCommand.CreateFromObject(
            objectSource: libs.WhenAnyValue(x => x.NewestSynthesisVersion)
                .ObserveOnGui(),
            canExecute: v =>
            {
                return Observable.CombineLatest(
                        this.WhenAnyValue(x => x.SynthesisVersioning),
                        this.WhenAnyValue(x => x.ManualSynthesisVersion),
                        v,
                        (versioning, manual, latest) =>
                        {
                            if (versioning != NugetVersioningEnum.Manual) return false;
                            return latest != null && latest != manual;
                        })
                    .ObserveOnGui();
            },
            execute: v => ManualSynthesisVersion = v ?? string.Empty,
            disposable: this);

        UpdateProfileNugetVersionCommand = CommandExt.CreateCombinedAny(
            this.UpdateMutagenManualToLatestCommand,
            this.UpdateSynthesisManualToLatestCommand);
    }
}