using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using System.IO;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.GUI
{
    public class PatcherSettingsVM : ViewModel
    {
        public ILogger Logger { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

        private readonly ObservableAsPropertyHelper<SettingsConfiguration> _SettingsConfiguration;
        public SettingsConfiguration SettingsConfiguration => _SettingsConfiguration.Value;

        private readonly ObservableAsPropertyHelper<bool> _SettingsOpen;
        public bool SettingsOpen => _SettingsOpen.Value;

        private readonly ObservableAsPropertyHelper<bool> _SettingsLoading;
        public bool SettingsLoading => _SettingsLoading.Value;

        private Lazy<IObservableCollection<ReflectionSettingsVM>> _reflectionSettings;
        public IObservableCollection<ReflectionSettingsVM> ReflectionSettings => _reflectionSettings.Value;

        public PatcherSettingsVM(
            ILogger logger,
            PatcherVM parent,
            IObservable<GetResponse<string>> projPath)
        {
            Logger = logger;
            _SettingsConfiguration = projPath
                .Select(i =>
                {
                    return Observable.Create<SettingsConfiguration>(async (observer, cancel) =>
                    {
                        observer.OnNext(new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig
                            >()));
                        if (i.Failed) return;

                        try
                        {
                            var result = await Synthesis.Bethesda.Execution.CLI.Commands.GetSettingsStyle(
                                i.Value,
                                directExe: false,
                                cancel: cancel);
                            observer.OnNext(result);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking if patcher can open settings: {ex}");
                        }
                    });
                })
                .Switch()
                .ToGuiProperty(this, nameof(SettingsConfiguration), new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>()));

            OpenSettingsCommand = NoggogCommand.CreateFromObject(
                objectSource: projPath,
                canExecute: x => x.Succeeded,
                extraCanExecute: this.WhenAnyValue(x => x.SettingsConfiguration)
                    .Select(x => x.Style == SettingsStyle.Open),
                execute: async (o) =>
                {
                    var result = await Synthesis.Bethesda.Execution.CLI.Commands.OpenForSettings(
                        o.Value,
                        directExe: false,
                        rect: parent.Profile.Config.MainVM.Rectangle,
                        cancel: CancellationToken.None);
                },
                disposable: this.CompositeDisposable);

            _SettingsOpen = OpenSettingsCommand.IsExecuting
                .ToGuiProperty(this, nameof(SettingsOpen));

            var targetSettingsVM = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SettingsConfiguration),
                    projPath,
                    (settingsTarget, projPath) => (settingsTarget, projPath))
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(i =>
                {
                    return Observable.Create<(bool Processing, ReflectionSettingsVM[] SettingsVM)>(async (observer, cancel) =>
                    {
                        if (i.projPath.Failed)
                        {
                            observer.OnNext((true, Array.Empty<ReflectionSettingsVM>()));
                            return;
                        }
                        else if (i.settingsTarget.Style != SettingsStyle.SpecifiedClass
                            || i.settingsTarget.Targets.Length == 0)
                        {
                            observer.OnNext((false, Array.Empty<ReflectionSettingsVM>()));
                            return;
                        }

                        observer.OnNext((true, Array.Empty<ReflectionSettingsVM>()));

                        try
                        {
                            var types = await Utility.ExtractTypesFromApp(
                                projPath: i.projPath.Value,
                                targetTypes: i.settingsTarget.Targets.Select(s => s.TypeName).ToArray(),
                                cancel: cancel);
                            if (types.Failed)
                            {
                                Logger.Error($"Error checking if patcher can open settings: {types.Reason}");
                                observer.OnNext((false, Array.Empty<ReflectionSettingsVM>()));
                                return;
                            }
                            var vms = await Task.WhenAll(types.Value.Select(async (t, index) =>
                            {
                                if (t == null) return null;
                                var settings = await ReflectionSettingsVM.Factory(
                                    t,
                                    i.settingsTarget.Targets[index].Nickname,
                                    Path.Combine(
                                        Path.Combine(Execution.Constants.TypicalExtraData, parent.DisplayName),
                                        i.settingsTarget.Targets[index].Path),
                                    logger,
                                    cancel);
                                return settings;
                            }));
                            observer.OnNext((false, vms.NotNull().ToArray()));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking if patcher can open settings: {ex}");
                            observer.OnNext((false, Array.Empty<ReflectionSettingsVM>()));
                            return;
                        }
                    });
                })
                .Switch()
                .Replay(1)
                .RefCount();

            _SettingsLoading = targetSettingsVM
                .Select(t => t.Processing)
                .ToGuiProperty(this, nameof(SettingsLoading), deferSubscription: true);

            _reflectionSettings = new Lazy<IObservableCollection<ReflectionSettingsVM>>(() =>
            {
                return targetSettingsVM
                   .Select(x =>
                   {
                       if (x.Processing)
                       {
                           return Enumerable.Empty<ReflectionSettingsVM>()
                               .AsObservableChangeSet(x => (StringCaseAgnostic)x.SettingsPath);
                       }
                       return x.SettingsVM.AsObservableChangeSet(x => (StringCaseAgnostic)x.SettingsPath);
                   })
                   .Switch()
                   .ToObservableCollection(this.CompositeDisposable);
            },
            isThreadSafe: true);
        }

        public void Persist(ILogger logger)
        {
            if (!_reflectionSettings.IsValueCreated) return;
            ReflectionSettings.ForEach(vm => vm.Persist(logger));
        }
    }
}
