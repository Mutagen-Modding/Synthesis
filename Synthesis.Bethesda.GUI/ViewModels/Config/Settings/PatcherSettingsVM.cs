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
using Mutagen.Bethesda.Synthesis.WPF;

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

        private bool _hasBeenRetrieved = false;
        private readonly ObservableAsPropertyHelper<ReflectionSettingsBundleVM?> _ReflectionSettings;
        public ReflectionSettingsBundleVM? ReflectionSettings
        {
            get
            {
                _hasBeenRetrieved = true;
                return _ReflectionSettings.Value;
            }
        }

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
                        cancel: CancellationToken.None,
                        release: parent.Profile.Release,
                        dataFolderPath: parent.Profile.DataFolder,
                        loadOrder: parent.Profile.LoadOrder.Items.Select(lvm => lvm.Listing));
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
                    return Observable.Create<(bool Processing, GetResponse<ReflectionSettingsBundleVM> SettingsVM)>(async (observer, cancel) =>
                    {
                        if (i.projPath.Failed
                            || i.settingsTarget.Style != SettingsStyle.SpecifiedClass
                            || i.settingsTarget.Targets.Length == 0)
                        {
                            observer.OnNext((false, GetResponse<ReflectionSettingsBundleVM>.Succeed(new ReflectionSettingsBundleVM())));
                            return;
                        }

                        observer.OnNext((true, new ReflectionSettingsBundleVM()));

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
                                observer.OnNext((false, vms.BubbleFailure<ReflectionSettingsBundleVM>()));
                                return;
                            }
                            await Task.WhenAll(vms.Value.Item.Select(vm => vm.Import(logger.Information, cancel)));
                            observer.OnNext((false, new ReflectionSettingsBundleVM(vms.Value.Item, vms.Value.Temp)));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error creating reflection GUI: {ex}");
                            observer.OnNext((false, GetResponse<ReflectionSettingsBundleVM>.Fail(ex)));
                        }
                        observer.OnCompleted();
                    });
                })
                .Switch()
                .DisposePrevious()
                .Replay(1)
                .RefCount();

            _SettingsLoading = targetSettingsVM
                .Select(t => t.Processing)
                .ToGuiProperty(this, nameof(SettingsLoading), deferSubscription: true);

            _ReflectionSettings = targetSettingsVM
                .Select(x =>
                {
                    if (x.Processing || x.SettingsVM.Failed)
                    {
                        return new ReflectionSettingsBundleVM();
                    }
                    return x.SettingsVM.Value;
                })
                .ObserveOnGui()
                .Select(x =>
                {
                    SelectedSettings = x.Settings?.FirstOrDefault();
                    return x;
                })
                .DisposePrevious()
                .ToGuiProperty<ReflectionSettingsBundleVM?>(this, nameof(ReflectionSettings), initialValue: null, deferSubscription: true);

            _Error = targetSettingsVM
                .Select(x => (ErrorResponse)x.SettingsVM)
                .ToGuiProperty(this, nameof(Error), deferSubscription: true);
        }

        public void Persist(Action<string> logger)
        {
            if (!_hasBeenRetrieved) return;
            ReflectionSettings?.Settings?.ForEach(vm => vm.Persist(logger));
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_hasBeenRetrieved)
            {
                ReflectionSettings?.Dispose();
            }
        }
    }
}
