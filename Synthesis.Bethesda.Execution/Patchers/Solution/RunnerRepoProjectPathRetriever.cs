using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

[ExcludeFromCodeCoverage]
public record ProjectPaths(FilePath FullPath, string SubPath);
    
public interface IRunnerRepoProjectPathRetriever
{
    ProjectPaths? Get(FilePath solutionPath, FilePath projPath);
}

public class RunnerRepoProjectPathRetriever : IRunnerRepoProjectPathRetriever
{
    private readonly IFileSystem _fileSystem;
    public IRunnerRepoDirectoryProvider RunnerRepoDirectoryProvider { get; }
    public IAvailableProjectsRetriever AvailableProjectsRetriever { get; }

    public RunnerRepoProjectPathRetriever(
        IFileSystem fileSystem,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        IAvailableProjectsRetriever availableProjectsRetriever)
    {
        _fileSystem = fileSystem;
        RunnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
        AvailableProjectsRetriever = availableProjectsRetriever;
    }
        
    public ProjectPaths? Get(FilePath solutionPath, FilePath projPath)
    {
        var projName = _fileSystem.Path.GetFileName(projPath);
        var str = AvailableProjectsRetriever.Get(solutionPath)
            .FirstOrDefault(av => _fileSystem.Path.GetFileName(av).Equals(projName));
        if (str == null) return null;
        return new ProjectPaths(
            new FilePath(
                Path.Combine(RunnerRepoDirectoryProvider.Path, str)),
            str);
    }
}