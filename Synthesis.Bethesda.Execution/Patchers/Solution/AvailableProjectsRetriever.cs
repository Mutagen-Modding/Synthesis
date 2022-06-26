using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Buildalyzer;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface IAvailableProjectsRetriever
{
    IEnumerable<string> Get(FilePath solutionPath);
}

[ExcludeFromCodeCoverage]
public class AvailableProjectsRetriever : IAvailableProjectsRetriever
{
    public IFileSystem FileSystem { get; }

    public AvailableProjectsRetriever(
        IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }
        
    public IEnumerable<string> Get(FilePath solutionPath)
    {
        if (!FileSystem.File.Exists(solutionPath)) return Enumerable.Empty<string>();
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