using System.IO;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IWorkingDirectoryProvider
    {
        DirectoryPath WorkingDirectory { get; }
    }

    public class WorkingDirectoryProvider : IWorkingDirectoryProvider
    {
        public DirectoryPath WorkingDirectory => Path.Combine(Path.GetTempPath(), "Synthesis")!;
    }
}