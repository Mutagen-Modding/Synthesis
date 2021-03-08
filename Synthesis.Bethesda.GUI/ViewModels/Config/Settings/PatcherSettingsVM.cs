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
using ReactiveUI.Fody.Helpers;

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

        private readonly Lazy<IObservableCollection<ReflectionSettingsVM>> _reflectionSettings;
        public IObservableCollection<ReflectionSettingsVM> ReflectionSettings => _reflectionSettings.Value;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _Error;
        public ErrorResponse Error => _Error.Value;

        [Reactive]
        public ReflectionSettingsVM? SelectedSettings { get; set; }

        public PatcherSettingsVM(
            ILogger logger,
            PatcherVM parent,
            IObservable<GetResponse<string>> projPath,
            bool needBuild)
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
                                cancel: cancel,
                                build: needBuild,
                                logger.Information);
                            logger.Information($"Settings type: {result}");
                            observer.OnNext(result);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking if patcher can open settings: {ex}");
                        }
                        observer.OnCompleted();
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
                    return Observable.Create<(bool Processing, GetResponse<ReflectionSettingsVM[]> SettingsVM)>(async (observer, cancel) =>
                    {
                        if (i.projPath.Failed
                            || i.settingsTarget.Style != SettingsStyle.SpecifiedClass
                            || i.settingsTarget.Targets.Length == 0)
                        {
                            observer.OnNext((false, GetResponse<ReflectionSettingsVM[]>.Succeed(Array.Empty<ReflectionSettingsVM>())));
                            return;
                        }

                        observer.OnNext((true, Array.Empty<ReflectionSettingsVM>()));

                        try
                        {
                            var vms = await Utility.ExtractInfoFromProject<ReflectionSettingsVM[]>(
                                projPath: i.projPath.Value,
                                cancel: cancel,
                                getter: (assemb) =>
                                {
                                    return i.settingsTarget.Targets
                                        .Select((s, index) =>
                                        {
                                            try
                                            {
                                                var t = assemb.GetType(s.TypeName);
                                                if (t == null) return null;
                                                return new ReflectionSettingsVM(
                                                    new SettingsParameters(
                                                        assemb, 
                                                        parent.Profile.LoadOrder.Connect(),
                                                        parent.Profile.SimpleLinkCache,
                                                        t,
                                                        Activator.CreateInstance(t),
                                                        MainVM: null!,
                                                        Parent: null),
                                                    nickname: i.settingsTarget.Targets[index].Nickname,
                                                    settingsFolder: Path.Combine(Execution.Paths.TypicalExtraData, parent.DisplayName),
                                                    settingsSubPath: i.settingsTarget.Targets[index].Path);
                                            }
                                            catch (Exception ex)
                                            {
                                                Logger.Error(ex.ToString());
                                                throw new ArgumentException($"Error creating reflected settings: {ex.Message}");
                                            }
                                        })
                                        .NotNull()
                                        .ToArray();
                                },
                                Logger.Information);
                            if (vms.Failed)
                            {
                                Logger.Error($"Error creating reflection GUI: {vms.Reason}");
                                observer.OnNext((false, vms.BubbleFailure<ReflectionSettingsVM[]>()));
                                return;
                            }
                            await Task.WhenAll(vms.Value.Select(vm => vm.Import(logger, cancel)));
                            observer.OnNext((false, vms.Value));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error creating reflection GUI: {ex}");
                            observer.OnNext((false, GetResponse<ReflectionSettingsVM[]>.Fail(ex)));
                        }
                        observer.OnCompleted();
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
                       if (x.Processing || x.SettingsVM.Failed)
                       {
                           return Enumerable.Empty<ReflectionSettingsVM>();
                       }
                       return x.SettingsVM.Value;
                   })
                   .ObserveOnGui()
                   .Select(x =>
                   {
                       SelectedSettings = x.FirstOrDefault();
                       return x.AsObservableChangeSet(x => (StringCaseAgnostic)x.SettingsSubPath);
                   })
                   .Switch()
                   .ToObservableCollection(this.CompositeDisposable);
            },
            isThreadSafe: true);

            _Error = targetSettingsVM
                .Select(x => (ErrorResponse)x.SettingsVM)
                .ToGuiProperty(this, nameof(Error), deferSubscription: true);
        }

        public void Persist(ILogger logger)
        {
            if (!_reflectionSettings.IsValueCreated) return;
            ReflectionSettings.ForEach(vm => vm.Persist(logger));
        }
    }
}
