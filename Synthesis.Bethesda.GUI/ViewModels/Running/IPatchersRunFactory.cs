using Serilog;

namespace Synthesis.Bethesda.GUI
{
    public interface IPatchersRunFactory
    {
        PatchersRunVM Create(ConfigurationVM configuration, ProfileVM profile, ILogger logger);
    }
}