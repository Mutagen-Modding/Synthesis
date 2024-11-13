using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class RunAPatcherTests
{
    [Theory, SynthAutoData]
    public async Task PrepExceptionReturnsNull(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunAPatcher sut)
    {
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await sut.Run(
                groupRun,
                new PatcherPrepBundle(
                    patcher,
                    Task.FromResult<Exception?>(new NotImplementedException())),
                cancellation,
                sourcePath,
                runParameters);
        });
    }
        
    [Theory, SynthAutoData]
    public async Task CancelledReturnsNull(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancelled,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunAPatcher sut)
    {
        (await sut.Run(
                groupRun,
                new PatcherPrepBundle(
                    patcher,
                    Task.FromResult<Exception?>(new NotImplementedException())),
                cancelled,
                sourcePath,
                runParameters))
            .Should().BeNull();
    }
        
    [Theory, SynthAutoData]
    public async Task RetrievesArgs(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunSynthesisPatcher args,
        RunAPatcher sut)
    {
        sut.GetRunArgs.GetArgs(default!, default!, default, default!)
            .ReturnsForAnyArgs(args);
        await sut.Run(
            groupRun,
            new PatcherPrepBundle(
                patcher,
                Task.FromResult<Exception?>(null)),
            cancellation,
            sourcePath,
            runParameters);
        sut.GetRunArgs.Received(1).GetArgs(
            groupRun,
            patcher,
            sourcePath,
            runParameters);
    }
        
    [Theory, SynthAutoData]
    public async Task PassesArgsToRun(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        ModPath outputPath,
        RunParameters runParameters,
        RunSynthesisPatcher args,
        RunAPatcher sut)
    {
        args.OutputPath = outputPath;
        sut.GetRunArgs.GetArgs(default!, default!, default, default!)
            .ReturnsForAnyArgs(args);
        await sut.Run(
            groupRun,
            new PatcherPrepBundle(
                patcher,
                Task.FromResult<Exception?>(null)),
            cancellation,
            sourcePath,
            runParameters);
        await patcher.Received(1).Run(args, cancellation);
    }
        
    [Theory, SynthAutoData]
    public async Task GetArgsThrowsShouldThrow(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunAPatcher sut)
    {
        sut.GetRunArgs.GetArgs(default!, default!, default, default!)
            .ThrowsForAnyArgs<NotImplementedException>();
        await Assert.ThrowsAsync<NotImplementedException>(() =>
        {
            return sut.Run(
                groupRun,
                new PatcherPrepBundle(
                    patcher,
                    Task.FromResult<Exception?>(null)),
                cancellation,
                sourcePath,
                runParameters);
        });
    }
        
    [Theory, SynthAutoData]
    public async Task RunThrowsShouldThrow(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunAPatcher sut)
    {
        patcher.Run(default!, default)
            .ThrowsForAnyArgs<NotImplementedException>();
        await Assert.ThrowsAsync<NotImplementedException>(() =>
        {
            return sut.Run(
                groupRun,
                new PatcherPrepBundle(
                    patcher,
                    Task.FromResult<Exception?>(null)),
                cancellation,
                sourcePath,
                runParameters);
        });
    }

    [Theory, SynthAutoData]
    public async Task PassesArgsToFinalize(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters,
        RunSynthesisPatcher args,
        FilePath outputPath,
        RunAPatcher sut)
    {
        sut.GetRunArgs.GetArgs(default!, default!, default, default!)
            .ReturnsForAnyArgs(args);
        args.OutputPath = outputPath;
        await sut.Run(
            groupRun,
            new PatcherPrepBundle(
                patcher,
                Task.FromResult<Exception?>(null)),
            cancellation,
            sourcePath,
            runParameters);
        sut.FinalizePatcherRun.Received(1)
            .Finalize(patcher, args.OutputPath);
    }

    [Theory, SynthAutoData]
    public async Task ReturnsFinalizedResults(
        IGroupRun groupRun,
        IPatcherRun patcher,
        CancellationToken cancellation,
        FilePath? sourcePath,
        RunParameters runParameters,
        FilePath ret,
        RunAPatcher sut)
    {
        sut.FinalizePatcherRun.Finalize(default!, default)
            .ReturnsForAnyArgs(ret);
        (await sut.Run(
                groupRun,
                new PatcherPrepBundle(
                    patcher,
                    Task.FromResult<Exception?>(null)),
                cancellation,
                sourcePath,
                runParameters))
            .Should().Be(ret);
    }
}