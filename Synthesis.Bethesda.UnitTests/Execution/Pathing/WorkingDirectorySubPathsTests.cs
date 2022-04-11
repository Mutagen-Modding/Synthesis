using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Pathing;

public class WorkingDirectorySubPathsTests
{
    [Theory, SynthAutoData]
    public void LoadingDirectory(
        DirectoryPath workingDir,
        WorkingDirectorySubPaths sut)
    {
        sut.WorkingDir.WorkingDirectory.Returns(workingDir);
        sut.LoadingFolder.Should().Be(
            new DirectoryPath(
                Path.Combine(workingDir, "Loading")));
    }
        
    [Theory, SynthAutoData]
    public void ProfileWorkingDirectory(
        DirectoryPath workingDir,
        string id,
        WorkingDirectorySubPaths sut)
    {
        sut.WorkingDir.WorkingDirectory.Returns(workingDir);
        sut.ProfileWorkingDirectory(id).Should().Be(
            new DirectoryPath(
                Path.Combine(workingDir, id, "Workspace")));
    }
}