using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class ExecuteRunTests
{
    [Theory, SynthAutoData]
    public async Task ResetsWorkingDirectory(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters);
            
        sut.ResetWorkingDirectory.Received(1).Reset();
    }
        
    [Theory, SynthAutoData]
    public async Task NoPatchersShortCircuits(
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        IGroupRun[] groups = Array.Empty<IGroupRun>();
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters);
        sut.EnsureSourcePathExists.DidNotReceiveWithAnyArgs().Ensure(default);
        await sut.RunAllGroups.DidNotReceiveWithAnyArgs().Run(
            default!, default, default, default!);
    }
        
    [Theory, SynthAutoData]
    public async Task CancellationThrows(
        CancellationToken cancelled,
        DirectoryPath outputDir,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        IGroupRun[] groups = Array.Empty<IGroupRun>();
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Run(
            groups,
            cancelled,
            outputDir,
            runParameters));
    }
}