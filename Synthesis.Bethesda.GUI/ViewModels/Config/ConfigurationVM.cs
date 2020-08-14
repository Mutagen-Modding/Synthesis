using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationVM : ViewModel
    {
        public MainVM MainVM { get; }

        public SourceCache<ProfileVM, string> Profiles { get; } = new SourceCache<ProfileVM, string>(p => p.ID);

        public IObservableCollection<ProfileVM> ProfilesDisplay { get; }
        public IObservableCollection<PatcherVM> PatchersDisplay { get; }

        public ICommand CompleteConfiguration { get; }
        public ICommand CancelConfiguration { get; }
        public ICommand ShowHelpToggleCommand { get; }

        public ReactiveCommandBase<Unit, Unit> RunPatchers { get; }

        [Reactive]
        public ProfileVM? SelectedProfile { get; set; }

        [Reactive]
        public PatcherVM? SelectedPatcher { get; set; }

        [Reactive]
        public PatcherVM? NewPatcher { get; set; }

        private readonly ObservableAsPropertyHelper<PatcherVM?> _DisplayedPatcher;
        public PatcherVM? DisplayedPatcher => _DisplayedPatcher.Value;

        private readonly ObservableAsPropertyHelper<PatchersRunVM?> _CurrentRun;
        public PatchersRunVM? CurrentRun => _CurrentRun.Value;

        [Reactive]
        public string WorkingDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "Synthesis");

        [Reactive]
        public bool ShowHelp { get; set; }

        public ConfigurationVM(MainVM mvm)
        {
            MainVM = mvm;
            ProfilesDisplay = Profiles.Connect().ToObservableCollection(this);
            PatchersDisplay = this.WhenAnyValue(x => x.SelectedProfile)
                .Select(p => p?.Patchers.Connect() ?? Observable.Empty<IChangeSet<PatcherVM>>())
                .Switch()
                .ToObservableCollection(this);

            CompleteConfiguration = ReactiveCommand.Create(
                () =>
                {
                    var patcher = this.NewPatcher;
                    if (patcher == null) return;
                    SelectedProfile?.Patchers.Add(patcher);
                    NewPatcher = null;
                    SelectedPatcher = patcher;
                    patcher.IsOn = true;
                },
                canExecute: this.WhenAnyValue(x => x.NewPatcher)
                    .Select(patcher =>
                    {
                        if (patcher == null) return Observable.Return(false);
                        return patcher.WhenAnyValue(x => x.CanCompleteConfiguration)
                            .Select(e => e.Succeeded);
                    })
                    .Switch());

            CancelConfiguration = ReactiveCommand.Create(
                () =>
                {
                    // Just forget about patcher and let it GC
                    NewPatcher = null;
                });

            _DisplayedPatcher = this.WhenAnyValue(
                    x => x.SelectedPatcher,
                    x => x.NewPatcher,
                    (selected, newConfig) => newConfig ?? selected)
                .ToGuiProperty(this, nameof(DisplayedPatcher));

            RunPatchers = NoggogCommand.CreateFromJob(
                extraInput: this.WhenAnyValue(x => x.SelectedProfile),
                jobCreator: (profile) =>
                {
                    if (SelectedProfile == null)
                    {
                        return (default(PatchersRunVM?), Observable.Return(Unit.Default));
                    }
                    var ret = new PatchersRunVM(this, SelectedProfile);
                    var completeSignal = ret.WhenAnyValue(x => x.Running)
                        .TurnedOff()
                        .FirstAsync();
                    return (ret, completeSignal);
                },
                createdJobs: out var createdRuns,
                canExecute: this.WhenAnyFallback(x => x.SelectedProfile!.BlockingError, fallback: ErrorResponse.Failure)
                    .Select(err => err.Succeeded))
                .DisposeWith(this);

            _CurrentRun = createdRuns
                .ToGuiProperty(this, nameof(CurrentRun));

            this.WhenAnyValue(x => x.CurrentRun)
                .NotNull()
                .Do(run => MainVM.ActivePanel = run)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(r => r.Run())
                .DisposeWith(this);

            ShowHelpToggleCommand = ReactiveCommand.Create(() => ShowHelp = !ShowHelp);
        }

        public void Load(SynthesisGuiSettings settings)
        {
            Profiles.Clear();
            Profiles.AddOrUpdate(settings.ExecutableSettings.Profiles.Select(p =>
            {
                return new ProfileVM(this, p);
            }));
            if (Profiles.TryGetValue(settings.ExecutableSettings.SelectedProfile, out var profile))
            {
                SelectedProfile = profile;
            }
            ShowHelp = settings.ShowHelp;
        }

        public SynthesisGuiSettings Save()
        {
            return new SynthesisGuiSettings()
            {
                ExecutableSettings = new SynthesisSettings()
                {
                    Profiles = Profiles.Items.Select(p => p.Save()).ToList(),
                    SelectedProfile = SelectedProfile?.ID ?? string.Empty
                },
                ShowHelp = ShowHelp
            };
        }
    }
}
