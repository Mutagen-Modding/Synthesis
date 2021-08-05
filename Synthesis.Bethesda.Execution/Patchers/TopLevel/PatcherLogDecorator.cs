using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Patchers.TopLevel
{
    public class PatcherLogDecorator : ILogger
    {
        private readonly ILogger _logger;

        public PatcherLogDecorator(
            IProfileIdentifier profileIdentifier,
            IPatcherNameProvider nameProvider)
        {
            _logger = Log.Logger
                .ForContext(FunnelNames.Patcher, nameProvider.Name)
                .ForContext(FunnelNames.Profile, profileIdentifier.Name);
        }
        
        public void Write(LogEvent logEvent)
        {
            _logger.Write(logEvent);
        }
    }
}