using System;
using System.IO;
using System.IO.Abstractions;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IWorkingDirectorySubPaths
    {
        string LoadingFolder { get; }
        string ProfileWorkingDirectory(string id);
    }

    public class WorkingDirectorySubPaths : IWorkingDirectorySubPaths
    {
        private readonly IWorkingDirectoryProvider _WorkingDir;
        public string LoadingFolder => Path.Combine(_WorkingDir.WorkingDirectory, "Loading");
        public string ProfileWorkingDirectory(string id) => Path.Combine(_WorkingDir.WorkingDirectory, id, "Workspace");

        public WorkingDirectorySubPaths(
            IWorkingDirectoryProvider workingDir)
        {
            _WorkingDir = workingDir;
        }
    }
}
