using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.Services.Profile
{
    public class ProfileLogDecorator : ViewModel, ILogger
    {
        private readonly ObservableAsPropertyHelper<ILogger> _logger;
        public ILogger Logger => _logger.Value;

        public ProfileLogDecorator(IProfileNameVm nameProvider)
        {
            _logger = nameProvider.WhenAnyValue(x => x.Name)
                .Select(x => Log.Logger.ForContext(FunnelNames.Profile, x))
                .ToGuiProperty(this, nameof(Logger), Log.Logger.ForContext(FunnelNames.Profile, nameProvider.Name), deferSubscription: true);
        }

        public void Write(LogEvent logEvent)
        {
            Logger.Write(logEvent);
        }
    }
}