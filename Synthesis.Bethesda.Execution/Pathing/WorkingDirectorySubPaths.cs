using System;
using System.IO;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IWorkingDirectorySubPaths
    {
        string LoadingFolder { get; }
        string ProfileWorkingDirectory(string id);
    }

    public class WorkingDirectorySubPaths : IWorkingDirectorySubPaths
    {
        private readonly IProvideWorkingDirectory _WorkingDir;
        public string LoadingFolder => Path.Combine(_WorkingDir.WorkingDirectory, "Loading");
        public string ProfileWorkingDirectory(string id) => Path.Combine(_WorkingDir.WorkingDirectory, id, "Workspace");

        public WorkingDirectorySubPaths(
            IProvideWorkingDirectory workingDir)
        {
            _WorkingDir = workingDir;
        }
    }
}
