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
        public PatcherInitVM? NewPatcher { get; set; }

        private readonly ObservableAsPropertyHelper<object?> _DisplayedObject;
        public object? DisplayedObject => _DisplayedObject.Value;

        private readonly ObservableAsPropertyHelper<PatchersRunVM?> _CurrentRun;
        public PatchersRunVM? CurrentRun => _CurrentRun.Value;

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

            CompleteConfiguration = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var initializer = this.NewPatcher;
                    if (initializer == null) return;
                    AddNewPatchers(await initializer.Construct().ToListAsync());
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
                    NewPatcher?.Cancel();
                    NewPatcher = null;
                });

            // Dispose any old patcher initializations
            this.WhenAnyValue(x => x.NewPatcher)
                .DisposePrevious()
                .Subscribe()
                .DisposeWith(this);

            _DisplayedObject = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SelectedProfile!.SelectedPatcher),
                    this.WhenAnyValue(x => x.NewPatcher),
                    (selected, newConfig) => (newConfig as object) ?? selected)
                .ToGuiProperty(this, nameof(DisplayedObject), default);

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
                .ToGuiProperty(this, nameof(CurrentRun), default);

            this.WhenAnyValue(x => x.CurrentRun)
                .NotNull()
                .Do(run => MainVM.ActivePanel = run)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(r => r.Run())
                .DisposeWith(this);

            ShowHelpToggleCommand = ReactiveCommand.Create(() => ShowHelp = !ShowHelp);
        }

        public void Load(SynthesisGuiSettings settings, PipelineSettings pipeSettings)
        {
            Profiles.Clear();
            Profiles.AddOrUpdate(pipeSettings.Profiles.Select(p =>
            {
                return new ProfileVM(this, p);
            }));
            if (Profiles.TryGetValue(settings.SelectedProfile, out var profile))
            {
                SelectedProfile = profile;
            }
            ShowHelp = settings.ShowHelp;
        }

        public void Save(out SynthesisGuiSettings guiSettings, out PipelineSettings pipeSettings)
        {
            pipeSettings = new PipelineSettings()
            {
                Profiles = Profiles.Items.Select(p => p.Save()).ToList(),
            };
            guiSettings = new SynthesisGuiSettings()
            {
                ShowHelp = ShowHelp,
                SelectedProfile = SelectedProfile?.ID ?? string.Empty
            };
        }

        public void AddNewPatchers(List<PatcherVM> patchersToAdd)
        {
            NewPatcher = null;
            if (patchersToAdd.Count == 0) return;
            if (SelectedProfile == null)
            {
                throw new ArgumentNullException("Selected profile unexpectedly null");
            }
            patchersToAdd.ForEach(p => p.IsOn = true);
            SelectedProfile.Patchers.AddRange(patchersToAdd);
            SelectedProfile.SelectedPatcher = patchersToAdd.First();
        }
    }
}
