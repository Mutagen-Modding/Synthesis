using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace Synthesis.Bethesda.GUI
{
    public class PatcherSettingsVM : ViewModel
    {
        record SettingsTypeState(bool Processing, Type? Type);

        public ILogger Logger { get; }

        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }

        private readonly ObservableAsPropertyHelper<SettingsTarget> _SettingsTarget;
        public SettingsTarget SettingsTarget => _SettingsTarget.Value;

        private readonly ObservableAsPropertyHelper<bool> _SettingsOpen;
        public bool SettingsOpen => _SettingsOpen.Value;

        private readonly ObservableAsPropertyHelper<bool> _SettingsLoading;
        public bool SettingsLoading => _SettingsLoading.Value;

        public PatcherSettingsVM(
            ILogger logger,
            PatcherVM parent,
            IObservable<GetResponse<string>> projPath)
        {
            Logger = logger;
            _SettingsTarget = projPath
                .Select(i =>
                {
                    return Observable.Create<SettingsTarget>(async (observer, cancel) =>
                    {
                        observer.OnNext(new SettingsTarget(SettingsStyle.None, null));
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
                .ToGuiProperty(this, nameof(SettingsTarget), new SettingsTarget(SettingsStyle.None, null));

            OpenSettingsCommand = NoggogCommand.CreateFromObject(
                objectSource: projPath,
                canExecute: x => x.Succeeded,
                extraCanExecute: this.WhenAnyValue(x => x.SettingsTarget)
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

            var settingsType = Observable.CombineLatest(
                    this.WhenAnyValue(x => x.SettingsTarget),
                    projPath,
                    (settingsTarget, projPath) => (settingsTarget, projPath))
                .Select(i =>
                {
                    return Observable.Create<SettingsTypeState>(async (observer, cancel) =>
                    {
                        if (i.projPath.Failed)
                        {
                            observer.OnNext(new SettingsTypeState(true, default(Type?)));
                            return;
                        }
                        else if (i.settingsTarget.Style != SettingsStyle.SpecifiedClass
                            || i.settingsTarget.SettingsType == null)
                        {
                            observer.OnNext(new SettingsTypeState(false, default(Type?)));
                            return;
                        }

                        observer.OnNext(new SettingsTypeState(true, default(Type?)));

                        try
                        {
                            var type = await Utility.ExtractTypeFromApp(
                                projPath: i.projPath.Value,
                                targetType: i.settingsTarget.SettingsType,
                                cancel: cancel);
                            if (type.Succeeded)
                            {
                                observer.OnNext(new SettingsTypeState(false, type.Value));
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking if patcher can open settings: {ex}");
                        }
                        observer.OnNext(new SettingsTypeState(false, default(Type?)));
                    });
                })
                .Switch();

            _SettingsLoading = settingsType
                .Select(t => t.Processing)
                .ToGuiProperty(this, nameof(SettingsLoading));
        }
    }
}
