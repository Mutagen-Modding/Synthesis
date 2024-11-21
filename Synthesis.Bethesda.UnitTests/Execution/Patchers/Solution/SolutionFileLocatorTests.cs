using System.IO.Abstractions.TestingHelpers;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution;

public class SolutionFileLocatorTests
{
    [Theory, SynthAutoData]
    public void FindsFirstSlnInDirectory(
        [Frozen]MockFileSystem fs,
        DirectoryPath existingRepoPath,
        SolutionFileLocator sut)
    {
        fs.File.Create(Path.Combine(existingRepoPath, "SomeFile.txt"));
        var pathToFind = new FilePath(Path.Combine(existingRepoPath, "SomeSln.sln"));
        fs.File.Create(pathToFind);
        fs.File.Create(Path.Combine(existingRepoPath, "SomeSln2.sln"));

        sut.GetPath(existingRepoPath)
            .Should().Be(pathToFind);
    }
        
    [Theory, SynthAutoData]
    public void NoSolutionReturnsNull(
        [Frozen]MockFileSystem fs,
        DirectoryPath existingRepoPath,
        SolutionFileLocator sut)
    {
        fs.File.Create(Path.Combine(existingRepoPath, "SomeFile.txt"));

        sut.GetPath(existingRepoPath)
            .Should().BeNull();
    }
}