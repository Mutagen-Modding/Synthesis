using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Buildalyzer;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IAvailableProjectsRetriever
    {
        IEnumerable<string> Get(FilePath solutionPath);
    }

    public class AvailableProjectsRetriever : IAvailableProjectsRetriever
    {
        private readonly IFileSystem _fileSystem;

        public AvailableProjectsRetriever(
            IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        
        public IEnumerable<string> Get(FilePath solutionPath)
        {
            if (!_fileSystem.File.Exists(solutionPath)) return Enumerable.Empty<string>();
            try
            {
                var manager = new AnalyzerManager(solutionPath);
                return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(solutionPath)}\\"!));
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}