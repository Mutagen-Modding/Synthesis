using System.IO;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IProvideWorkingDirectory
    {
        string WorkingDirectory { get; }
    }

    public class ProvideWorkingDirectory : IProvideWorkingDirectory
    {
        public string WorkingDirectory => Path.Combine(Path.GetTempPath(), "Synthesis")!;
    }
}