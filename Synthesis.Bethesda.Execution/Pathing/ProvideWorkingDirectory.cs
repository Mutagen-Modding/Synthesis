using System.IO;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IProvideWorkingDirectory
    {
        DirectoryPath WorkingDirectory { get; }
    }

    public class ProvideWorkingDirectory : IProvideWorkingDirectory
    {
        public DirectoryPath WorkingDirectory => Path.Combine(Path.GetTempPath(), "Synthesis")!;
    }
}