using System;
using System.IO;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IWorkingDirectorySubPaths
    {
        string TypicalExtraData { get; }
        string LoadingFolder { get; }
        string ProfileWorkingDirectory(string id);
    }

    public class WorkingDirectorySubPaths : IWorkingDirectorySubPaths
    {
        private readonly IProvideWorkingDirectory _WorkingDir;
        public string TypicalExtraData => Path.Combine(Environment.CurrentDirectory, "Data");
        public string LoadingFolder => Path.Combine(_WorkingDir.WorkingDirectory, "Loading");
        public string ProfileWorkingDirectory(string id) => Path.Combine(_WorkingDir.WorkingDirectory, id, "Workspace");

        public WorkingDirectorySubPaths(
            IProvideWorkingDirectory workingDir)
        {
            _WorkingDir = workingDir;
        }
    }
}
