using System.Diagnostics.CodeAnalysis;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;

[ExcludeFromCodeCoverage]
public record DriverPaths(FilePath SolutionPath, List<string> AvailableProjects);

public interface IGetDriverPaths
{
    GetResponse<DriverPaths> Get();
}

public class GetDriverPaths : IGetDriverPaths
{
    public ISolutionFileLocator SolutionFileLocator { get; }
    public IDriverRepoDirectoryProvider DriverRepoDirectoryProvider { get; }
    public IAvailableProjectsRetriever AvailableProjectsRetriever { get; }

    public GetDriverPaths(
        ISolutionFileLocator solutionFileLocator,
        IDriverRepoDirectoryProvider driverRepoDirectoryProvider,
        IAvailableProjectsRetriever availableProjectsRetriever)
    {
        SolutionFileLocator = solutionFileLocator;
        DriverRepoDirectoryProvider = driverRepoDirectoryProvider;
        AvailableProjectsRetriever = availableProjectsRetriever;
    }
        
    public GetResponse<DriverPaths> Get()
    {
        var slnPath = SolutionFileLocator.GetPath(DriverRepoDirectoryProvider.Path);
        if (slnPath == null)
        {
            return GetResponse<DriverPaths>.Fail("Could not locate solution to run.");
        }

        var availableProjs = AvailableProjectsRetriever.Get(slnPath.Value).ToList();

        return new DriverPaths(slnPath.Value, availableProjs);
    }
}