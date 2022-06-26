namespace Synthesis.Bethesda.GUI.Services.Startup;

public interface IStartupTracker
{
    bool Initialized { get; set; }
}

public class StartupTracker : IStartupTracker
{
    public bool Initialized { get; set; }
}