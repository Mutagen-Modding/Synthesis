using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Solution;

public class CopyOverExtraDataTests
{
    [Theory, SynthAutoData]
    public void DeepCopyNotCalledIfDefaultDataMissing(
        DirectoryPath missingDir,
        DirectoryPath existingDir,
        CopyOverExtraData sut)
    {
        sut.DefaultDataPathProvider.Path.ReturnsForAnyArgs(missingDir);
        sut.UserExtraData.Path.ReturnsForAnyArgs(existingDir);
        sut.Copy();
        sut.DeepCopy.DidNotReceiveWithAnyArgs().DeepCopy(default, default);
    }
        
    [Theory, SynthAutoData]
    public void DeepCopyNotCalledIfUserExtraDataExists(
        DirectoryPath existingDir,
        CopyOverExtraData sut)
    {
        sut.DefaultDataPathProvider.Path.ReturnsForAnyArgs(existingDir);
        sut.UserExtraData.Path.ReturnsForAnyArgs(existingDir);
        sut.Copy();
        sut.DeepCopy.DidNotReceiveWithAnyArgs().DeepCopy(default, default);
    }
        
    [Theory, SynthAutoData]
    public void DeepCopyIfDefaultDataExistsAndUserDirectoryDoesNot(
        DirectoryPath existingInputDir,
        DirectoryPath missingOutputDir,
        CopyOverExtraData sut)
    {
        sut.DefaultDataPathProvider.Path.ReturnsForAnyArgs(existingInputDir);
        sut.UserExtraData.Path.ReturnsForAnyArgs(missingOutputDir);
        sut.Copy();
        sut.DeepCopy.Received(1).DeepCopy(existingInputDir, missingOutputDir);
    }
}