using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Git;

public class GitRunSolutionPathProviderTests
{
    [Theory, SynthAutoData]
    public void PassesProviderToLocator(
        DirectoryPath path,
        GitRunSolutionPathProvider sut)
    {
        sut.RunnerRepoDirectoryProvider.Path.Returns(path);
        var p = sut.Path;
        sut.SolutionFileLocator.Received(1).GetPath(path);
    }
        
    [Theory, SynthAutoData]
    public void ReturnsLocatorResultIfNotNull(
        FilePath path,
        GitRunSolutionPathProvider sut)
    {
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(path);
        sut.Path.Should().Be(path);
    }
        
    [Theory, SynthAutoData]
    public void NullLocatorResultThrows(
        GitRunSolutionPathProvider sut)
    {
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(default(FilePath?));
        Assert.Throws<FileNotFoundException>(() =>
        {
            var p = sut.Path;
        });
    }
}