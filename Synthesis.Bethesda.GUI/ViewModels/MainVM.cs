using DynamicData;
using DynamicData.Binding;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using Noggog;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Execution.Settings;
using System.Reactive;
using System.IO;
using Synthesis.Bethesda.Execution;

namespace Synthesis.Bethesda.GUI
{
    public class MainVM : ViewModel
    {
        public ConfigurationVM Configuration { get; }
        public SynthesisGuiSettings Settings { get; private set; } = new SynthesisGuiSettings();

        [Reactive]
        public ViewModel ActivePanel { get; set; }

        public ICommand ConfirmActionCommand { get; }
        public ICommand DiscardActionCommand { get; }
        public ICommand OpenProfilesPageCommand { get; }

        [Reactive]
        public ConfirmationActionVM? ActiveConfirmation { get; set; }

        public ObservableCollectionExtended<IDE> IdeOptions { get; } = new ObservableCollectionExtended<IDE>();

        [Reactive]
        public IDE Ide { get; set; }

        // Whether to show red glow in background
        private readonly ObservableAsPropertyHelper<bool> _Hot;
        public bool Hot => _Hot.Value;

        public string SynthesisVersion { get; }

        public string MutagenVersion { get; }

        public IObservable<string?> NewestSynthesisVersion { get; }
        public IObservable<string?> NewestMutagenVersion { get; }

        public MainVM()
        {
            Configuration = new ConfigurationVM(this);
            ActivePanel = Configuration;
            DiscardActionCommand = ReactiveCommand.Create(() => ActiveConfirmation = null);
            ConfirmActionCommand = ReactiveCommand.Create(
                () =>
                {
                    if (ActiveConfirmation == null) return;
                    ActiveConfirmation.ToDo();
                    ActiveConfirmation = null;
                });

            _Hot = this.WhenAnyValue(x => x.ActivePanel)
                .Select(x =>
                {
                    switch (x)
                    {
                        case ConfigurationVM config:
                            return config.WhenAnyFallback(x => x.CurrentRun!.Running, fallback: false);
                        case PatchersRunVM running:
                            return running.WhenAnyValue(x => x.Running);
                        default:
                            break;
                    }
                    return Observable.Return(false);
                })
                .Switch()
                .DistinctUntilChanged()
                .ToGuiProperty(this, nameof(Hot));

            OpenProfilesPageCommand = ReactiveCommand.Create(() =>
            {
                ActivePanel = new ProfilesDisplayVM(Configuration, ActivePanel);
            },
            canExecute: Observable.CombineLatest(
                    this.WhenAnyFallback(x => x.Configuration.CurrentRun!.Running, fallback: false),
                    this.WhenAnyValue(x => x.ActivePanel)
                        .Select(x => x is ProfilesDisplayVM),
                    (running, isProfile) => !running && !isProfile));

            IdeOptions.AddRange(EnumExt.GetValues<IDE>());

            Task.Run(() => Mutagen.Bethesda.WarmupAll.Init()).FireAndForget();

            SynthesisVersion = Mutagen.Bethesda.Synthesis.Constants.SynthesisVersion;
            MutagenVersion = Mutagen.Bethesda.Synthesis.Constants.MutagenVersion;

            var latestVersions = Observable.Return(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(_ => GetLatestVersions())
                .Replay(1)
                .RefCount();
            NewestMutagenVersion = latestVersions
                .Select(x => x.MutagenVersion);
            NewestSynthesisVersion = latestVersions
                .Select(x => x.SynthesisVersion);
        }

        public void Load(SynthesisGuiSettings? guiSettings, PipelineSettings? pipeSettings)
        {
            if (guiSettings != null)
            {
                Settings = guiSettings;
                Ide = guiSettings.Ide;
            }
            Configuration.Load(Settings, pipeSettings ?? new PipelineSettings());
        }

        public void Save(out SynthesisGuiSettings guiSettings, out PipelineSettings pipeSettings)
        {
            Configuration.Save(out guiSettings, out pipeSettings);
            guiSettings.Ide = this.Ide;
            guiSettings.MainRepositoryFolder = Settings.MainRepositoryFolder;
            guiSettings.OpenIdeAfterCreating = Settings.OpenIdeAfterCreating;
        }

        public void Init()
        {
            if (Configuration.Profiles.Count == 0)
            {
                ActivePanel = new NewProfileVM(this.Configuration, (profile) =>
                {
                    profile.Nickname = profile.Release.ToDescriptionString();
                    Configuration.SelectedProfile = profile;
                    Configuration.MainVM.ActivePanel = Configuration;
                });
            }
        }

        public async Task<(string? MutagenVersion, string? SynthesisVersion)> GetLatestVersions()
        {
            try
            {
                Log.Logger.Information("Querying for latest published library versions");
                var bootstrapProjectDir = new DirectoryPath(Path.Combine(Execution.Constants.WorkingDirectory, "VersionQuery"));
                bootstrapProjectDir.DeleteEntireFolder();
                bootstrapProjectDir.Create();
                var slnPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.sln");
                ASolutionInitializer.CreateSolutionFile(slnPath);
                var projPath = Path.Combine(bootstrapProjectDir.Path, "VersionQuery.csproj");
                ASolutionInitializer.CreateProject(projPath, GameCategory.Skyrim);
                ASolutionInitializer.AddProjectToSolution(slnPath, projPath);
                var ret = await NugetQuery.QueryVersions(projPath, current: false);
                Log.Logger.Information("Latest published library versions:");
                Log.Logger.Information($"  Mutagen: {ret.MutagenVersion}");
                Log.Logger.Information($"  Synthesis: {ret.SynthesisVersion}");
                return (ret.MutagenVersion ?? this.MutagenVersion, ret.SynthesisVersion ?? this.SynthesisVersion);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error querying for latest nuget versions");
                return (null, null);
            }
        }
    }
}
