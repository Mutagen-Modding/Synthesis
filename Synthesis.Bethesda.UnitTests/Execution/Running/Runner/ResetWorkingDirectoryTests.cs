using Noggog;
using Noggog.Testing.AutoFixture;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class ResetWorkingDirectoryTests
{
    [Theory, SynthAutoData]
    public void DeletesEntireFolder(
        DirectoryPath workingDirectory,
        ResetWorkingDirectory sut)
    {
        sut.ProfileDirectories.WorkingDirectory.Returns(workingDirectory);
        sut.Reset();
        sut.DeleteEntireDirectory.Received(1).DeleteEntireFolder(workingDirectory);
    }
        
    [Theory, SynthAutoData(FileSystem: TargetFileSystem.Substitute)]
    public void CreatesDirectory(
        DirectoryPath workingDirectory,
        ResetWorkingDirectory sut)
    {
        sut.ProfileDirectories.WorkingDirectory.Returns(workingDirectory);
        sut.Reset();
        sut.FileSystem.Directory.Received(1).CreateDirectory(workingDirectory);
    }
        
    [Theory, SynthAutoData(FileSystem: TargetFileSystem.Substitute)]
    public void DeletesBeforeCreates(
        ResetWorkingDirectory sut)
    {
        sut.Reset();
        Received.InOrder(() =>
        {
            sut.DeleteEntireDirectory.DeleteEntireFolder(Arg.Any<DirectoryPath>());
            sut.FileSystem.Directory.CreateDirectory(Arg.Any<string>());
        });
    }
}