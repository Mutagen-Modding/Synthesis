using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution;

public class SolutionPatcherPrepTests
{
    [Theory, SynthAutoData]
    public async Task CopiesOverExtraData(
        CancellationToken cancel,
        SolutionPatcherPrep sut)
    {
        await sut.Prep(cancel);
        sut.CopyOverExtraData.Received(1).Copy();
    }
        
    [Theory, SynthAutoData]
    public async Task PassesPathToProjToBuild(
        FilePath pathToProj,
        CancellationToken cancel,
        SolutionPatcherPrep sut)
    {
        sut.PathToProjProvider.Path.Returns(pathToProj);
        await sut.Prep(cancel);
        await sut.Build.Received(1).Compile(pathToProj, cancel);
    }
        
    [Theory, SynthAutoData]
    public async Task BuildFailureThrows(
        CancellationToken cancel,
        ErrorResponse fail,
        SolutionPatcherPrep sut)
    {
        sut.Build.Compile(default, default).ReturnsForAnyArgs(fail);
        await Assert.ThrowsAsync<SynthesisBuildFailure>(() => sut.Prep(cancel));
    }
}