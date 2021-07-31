using System.IO;
using System.Linq;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution
{
    public class RunnerRepoProjectPathRetrieverTests
    {
        [Theory, SynthAutoData]
        public void NoResultReturnsNull(
            FilePath solutionPath,
            FilePath projPath,
            RunnerRepoProjectPathRetriever sut)
        {
            sut.AvailableProjectsRetriever.Get(default).ReturnsForAnyArgs(Enumerable.Empty<string>());
            sut.Get(solutionPath, projPath)
                .Should().BeNull();
        }
        
        [Theory, SynthAutoData]
        public void PassesSolutionPathToAvailableProjectsRetriever(
            FilePath solutionPath,
            FilePath projPath,
            RunnerRepoProjectPathRetriever sut)
        {
            sut.Get(solutionPath, projPath);
            sut.AvailableProjectsRetriever.Received(1).Get(solutionPath);
        }
        
        [Theory, SynthAutoData]
        public void FindsFirstAvailableProjWithMatchingName(
            FilePath solutionPath,
            RunnerRepoProjectPathRetriever sut)
        {
            var runnerRepoPath = "D:/RunnerRepo";
            var projSubpath = "C:/Folder/SomeProj.csproj";
            sut.RunnerRepoDirectoryProvider.Path.Returns(new DirectoryPath(runnerRepoPath));
            sut.AvailableProjectsRetriever.Get(default).ReturnsForAnyArgs(new string[]
            {
                "SomeOtherProj/SomeOtherProj.csproj",
                "SomeDir/SomeProj.csproj",
            });
            var ret = sut.Get(solutionPath, projSubpath);
            ret!.SubPath.Should().Be("SomeDir/SomeProj.csproj");
            ret!.FullPath.Should().Be(new FilePath("D:/RunnerRepo/SomeDir/SomeProj.csproj"));
        }
    }
}