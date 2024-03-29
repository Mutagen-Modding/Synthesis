﻿using Noggog;
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
        FilePath? sourcePath,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters,
            sourcePath);
            
        sut.ResetWorkingDirectory.Received(1).Reset();
    }
        
    [Theory, SynthAutoData]
    public async Task NoPatchersShortCircuits(
        CancellationToken cancellation,
        FilePath? sourcePath,
        DirectoryPath outputDir,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        IGroupRun[] groups = Array.Empty<IGroupRun>();
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters,
            sourcePath);
        sut.EnsureSourcePathExists.DidNotReceiveWithAnyArgs().Ensure(default);
        await sut.RunAllGroups.DidNotReceiveWithAnyArgs().Run(
            default!, default, default, default!);
    }
        
    [Theory, SynthAutoData]
    public async Task CancellationThrows(
        CancellationToken cancelled,
        FilePath? sourcePath,
        DirectoryPath outputDir,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        IGroupRun[] groups = Array.Empty<IGroupRun>();
        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Run(
            groups,
            cancelled,
            outputDir,
            runParameters,
            sourcePath));
    }
        
    [Theory, SynthAutoData]
    public async Task EnsuresSourcePathExists(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        FilePath? sourcePath,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters,
            sourcePath);
            
        sut.EnsureSourcePathExists.Received(1).Ensure(sourcePath);
    }
        
    [Theory, SynthAutoData]
    public async Task ResetBeforeEnsure(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        FilePath? sourcePath,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters,
            sourcePath);
            
        Received.InOrder(() =>
        {
            sut.ResetWorkingDirectory.Reset();
            sut.EnsureSourcePathExists.Ensure(
                Arg.Any<FilePath?>());
        });
    }
        
    [Theory, SynthAutoData]
    public async Task EnsurePathExistsBeforeRun(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        FilePath? sourcePath,
        RunParameters runParameters,
        ExecuteRun sut)
    {
        await sut.Run(
            groups,
            cancellation,
            outputDir,
            runParameters,
            sourcePath);
            
        Received.InOrder(() =>
        {
            sut.EnsureSourcePathExists.Ensure(
                Arg.Any<FilePath?>());
            sut.RunAllGroups.Run(
                Arg.Any<IGroupRun[]>(), Arg.Any<CancellationToken>(), Arg.Any<DirectoryPath>(), 
                Arg.Any<RunParameters>(), Arg.Any<FilePath?>());
        });
    }
}