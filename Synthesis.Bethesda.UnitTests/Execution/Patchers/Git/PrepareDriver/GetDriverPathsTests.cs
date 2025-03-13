using Shouldly;
using Noggog;
using Noggog.Testing.Extensions;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareDriver;

public class GetDriverPathsTests
{
    [Theory, SynthAutoData]
    public void DriverRepoPassedIntoSolutionFileLocator(
        DirectoryPath localPath,
        GetDriverPaths sut)
    {
        sut.DriverRepoDirectoryProvider.Path.Returns(localPath);
        sut.Get();
        sut.SolutionFileLocator.Received(1).GetPath(localPath);
    }
        
    [Theory, SynthAutoData]
    public void FailedSolutionFileLocatorFails(
        GetDriverPaths sut)
    {
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(default(FilePath?));
        sut.Get()
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void SolutionPathPassedToAvailableProjectsRetriever(
        FilePath path,
        GetDriverPaths sut)
    {
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(path);
        sut.Get();
        sut.AvailableProjectsRetriever.Received(1).Get(path);
    }
        
    [Theory, SynthAutoData]
    public void ExpectedReturns(
        FilePath path,
        string[] paths,
        GetDriverPaths sut)
    {
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(path);
        sut.AvailableProjectsRetriever.Get(default).ReturnsForAnyArgs(paths);
        var ret = sut.Get();
        ret.Succeeded.ShouldBeTrue();
        ret.Value.SolutionPath.ShouldBe(path);
        ret.Value.AvailableProjects.ShouldEqual(paths);
    }
}