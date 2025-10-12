using Noggog;

namespace Synthesis.Bethesda.GUI.Logging;

public interface ILogSettings
{
    DirectoryPath LogFolder { get; }
}

public class LogSettings : ILogSettings
{
    public DirectoryPath LogFolder => Log.LogFolder;
}