using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Versioning.Query;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Versioning;

public class QueryVersionProjectPathTests
{
    [Theory, SynthAutoData]
    public void BaseFolderUnderWorkingDirectory(
        DirectoryPath workingDir,
        QueryVersionProjectPathing sut)
    {
        sut.Paths.WorkingDirectory.Returns(workingDir);
        sut.BaseFolder.IsUnderneath(workingDir)
            .Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void SolutionFileUnderBaseFolder(
        DirectoryPath workingDir,
        QueryVersionProjectPathing sut)
    {
        sut.Paths.WorkingDirectory.Returns(workingDir);
        sut.SolutionFile.IsUnderneath(sut.BaseFolder)
            .Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public void ProjectFileUnderBaseFolder(
        DirectoryPath workingDir,
        QueryVersionProjectPathing sut)
    {
        sut.Paths.WorkingDirectory.Returns(workingDir);
        sut.ProjectFile.IsUnderneath(sut.BaseFolder)
            .Should().BeTrue();
    }
}