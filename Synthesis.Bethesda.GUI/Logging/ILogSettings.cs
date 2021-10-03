using Noggog;

namespace Synthesis.Bethesda.GUI.Logging
{
    public interface ILogSettings
    {
        DirectoryPath LogFolder { get; }
        string DateFormat { get; }
    }

    public class LogSettings : ILogSettings
    {
        public DirectoryPath LogFolder => Log.LogFolder;
        public string DateFormat => Log.DateFormat;
    }
}