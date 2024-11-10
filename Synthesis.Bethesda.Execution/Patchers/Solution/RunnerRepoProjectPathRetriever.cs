using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

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
    private readonly ILogger _logger;
    public IRunnerRepoDirectoryProvider RunnerRepoDirectoryProvider { get; }
    public IAvailableProjectsRetriever AvailableProjectsRetriever { get; }

    public RunnerRepoProjectPathRetriever(
        IFileSystem fileSystem,
        IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
        IAvailableProjectsRetriever availableProjectsRetriever,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        RunnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
        AvailableProjectsRetriever = availableProjectsRetriever;
    }
        
    public ProjectPaths? Get(FilePath solutionPath, FilePath projPath)
    {
        var projName = _fileSystem.Path.GetFileName(projPath);
        var availableProjs = AvailableProjectsRetriever.Get(solutionPath).ToArray();
        _logger.Information("Available projects: {Projects}", string.Join(", ", availableProjs));
        var str = availableProjs
            .FirstOrDefault(av => _fileSystem.Path.GetFileName(av).Equals(projName));
        if (str == null) return null;
        var path = new FilePath(
            Path.Combine(RunnerRepoDirectoryProvider.Path, str));
        _logger.Information("Using project {Project} at path {Path}", str, path);
        return new ProjectPaths(
            path,
            str);
    }
}