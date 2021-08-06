using System;
using System.Reactive;
using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.GUI.Services.Versioning;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins
{
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
            IProfileIdentifier ident,
            INewestLibraryVersions newestLibs)
        {
            ActiveVersioning = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.MutagenVersioning),
                    this.WhenAnyValue(x => x.ManualMutagenVersion),
                    newestLibs.NewestMutagenVersion,
                    this.WhenAnyValue(x => x.SynthesisVersioning),
                    this.WhenAnyValue(x => x.ManualSynthesisVersion),
                    newestLibs.NewestSynthesisVersion,
                    (mutaVersioning, mutaManual, newestMuta, synthVersioning, synthManual, newestSynth) =>
                    {
                        return new ActiveNugetVersioning(
                            new NugetsToUse("Mutagen", mutaVersioning, mutaManual ?? newestMuta ?? string.Empty, newestMuta),
                            new NugetsToUse("Synthesis", synthVersioning, synthManual ?? newestSynth ?? string.Empty, newestSynth));
                    })
                .Do(x => logger.Information("Swapped profile {Nickname} to {Versioning}", ident.Name, x))
                .ObserveOnGui()
                .Replay(1)
                .RefCount();

            // Set manual if empty
            newestLibs.NewestMutagenVersion
                .Subscribe(x =>
                {
                    if (ManualMutagenVersion == null)
                    {
                        ManualMutagenVersion = x;
                    }
                })
                .DisposeWith(this);
            newestLibs.NewestSynthesisVersion
                .Subscribe(x =>
                {
                    if (ManualSynthesisVersion == null)
                    {
                        ManualSynthesisVersion = x;
                    }
                })
                .DisposeWith(this);

            UpdateMutagenManualToLatestCommand = NoggogCommand.CreateFromObject(
                objectSource: newestLibs.NewestMutagenVersion
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
                objectSource: newestLibs.NewestSynthesisVersion
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
}