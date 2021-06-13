using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Serilog;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationVM : ViewModel
    {
        private ISelectedProfileControllerVm _SelectedProfileController;
        private readonly IProfileFactory _ProfileFactory;

        public SourceCache<ProfileVM, string> Profiles { get; } = new(p => p.ID);

        public IObservableCollection<ProfileVM> ProfilesDisplay { get; }
        public IObservableCollection<PatcherVM> PatchersDisplay { get; }

        public ReactiveCommandBase<Unit, Unit> RunPatchers { get; }

        private readonly ObservableAsPropertyHelper<ProfileVM?> _SelectedProfile;
        public ProfileVM? SelectedProfile => _SelectedProfile.Value;

        private readonly ObservableAsPropertyHelper<object?> _DisplayedObject;
        public object? DisplayedObject => _DisplayedObject.Value;

        private readonly ObservableAsPropertyHelper<PatchersRunVM?> _CurrentRun;
        public PatchersRunVM? CurrentRun => _CurrentRun.Value;
        
        public PatcherInitializationVM Init { get; }

        public ConfigurationVM(
            PatcherInitializationVM initVm,
            IActivePanelControllerVm activePanelController,
            ISelectedProfileControllerVm selectedProfile,
            ISaveSignal saveSignal,
            IProfileFactory profileFactory,
            ILogger logger)
        {
            logger.Information("Creating ConfigurationVM");
            Init = initVm;
            _SelectedProfileController = selectedProfile;
            _ProfileFactory = profileFactory;
            _SelectedProfile = _SelectedProfileController.WhenAnyValue(x => x.SelectedProfile)
                .ToGuiProperty(this, nameof(SelectedProfile), default);
            
            ProfilesDisplay = Profiles.Connect().ToObservableCollection(this);
            PatchersDisplay = this.WhenAnyValue(x => x.SelectedProfile)
                .Select(p => p?.Patchers.Connect() ?? Observable.Empty<IChangeSet<PatcherVM>>())
                .Switch()
                .ToObservableCollection(this);

            _DisplayedObject = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SelectedProfile!.DisplayController.SelectedObject),
                    initVm.WhenAnyValue(x => x.NewPatcher),
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
                    var ret = new PatchersRunVM(this, logger, activePanelController, SelectedProfile);
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
                .Do(run => activePanelController.ActivePanel = run)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Subscribe(r => r.Run())
                .DisposeWith(this);

            saveSignal.Saving
                .Subscribe(x => Save(x.Gui, x.Pipe))
                .DisposeWith(this);
        }

        public void Load(ISynthesisGuiSettings settings, IPipelineSettings pipeSettings)
        {
            Profiles.Clear();
            Profiles.AddOrUpdate(pipeSettings.Profiles.Select(p =>
            {
                return _ProfileFactory.Get(p);
            }));
            if (Profiles.TryGetValue(settings.SelectedProfile, out var profile))
            {
                _SelectedProfileController.SelectedProfile = profile;
            }
        }

        private void Save(SynthesisGuiSettings guiSettings, PipelineSettings pipeSettings)
        {
            guiSettings.SelectedProfile = SelectedProfile?.ID ?? string.Empty;
            pipeSettings.Profiles = Profiles.Items.Select(p => p.Save()).ToList<ISynthesisProfile>();
        }

        public override void Dispose()
        {
            base.Dispose();
            Profiles.Items.ForEach(p => p.Dispose());
        }
    }
}
