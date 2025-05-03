using Shouldly;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Pathing;

public class WorkingDirectorySubPathsTests
{
    [Theory, SynthAutoData]
    public void LoadingDirectory(
        DirectoryPath workingDir,
        WorkingDirectorySubPaths sut)
    {
        sut.WorkingDir.WorkingDirectory.Returns(workingDir);
        sut.LoadingFolder.ShouldBe(
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
        sut.ProfileWorkingDirectory(id).ShouldBe(
            new DirectoryPath(
                Path.Combine(workingDir, id, "Workspace")));
    }
}