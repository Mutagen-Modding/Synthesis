using Serilog;
using Serilog.Events;
using Synthesis.Bethesda.Execution.Logging;

namespace Synthesis.Bethesda.Execution.Profile
{
    public class ProfileLogDecorator : ILogger
    {
        private readonly ILogger _logger;

        public ProfileLogDecorator(IProfileIdentifier identifier)
        {
            _logger = Log.Logger.ForContext(FunnelNames.Profile, identifier.Name);
        }

        public void Write(LogEvent logEvent)
        {
            _logger.Write(logEvent);
        }
    }
}