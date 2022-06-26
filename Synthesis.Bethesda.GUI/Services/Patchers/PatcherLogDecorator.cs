using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.Services.Patchers;

public class PatcherLogDecorator : ViewModel, ILogger
{
    private readonly ObservableAsPropertyHelper<ILogger> _logger;
    public ILogger Logger => _logger.Value;

    public PatcherLogDecorator(
        IProfileNameVm profileNameProvider,
        IPatcherNameVm patcherNameVm)
    {
        _logger = Observable.CombineLatest(
                profileNameProvider.WhenAnyValue(x => x.Name),
                patcherNameVm.WhenAnyValue(x => x.Name),
                (profile, patcher) => Log.Logger
                    .ForContext(FunnelNames.Patcher, patcher)
                    .ForContext(FunnelNames.Profile, profile))
            .ToGuiProperty(this, nameof(Logger), Log.Logger
                .ForContext(FunnelNames.Patcher, patcherNameVm.Name)
                .ForContext(FunnelNames.Profile, profileNameProvider.Name), deferSubscription: true);
    }
        
    public void Write(LogEvent logEvent)
    {
        Logger.Write(logEvent);
    }
}