using System.ComponentModel;
using System.Diagnostics;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Noggog.Processes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Cli;

public class CliPatcherRunTests
{
    [Theory, SynthAutoData]
    public async Task PassesSettingsToConverter(
        RunSynthesisPatcher runSettings,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default!, default).ReturnsForAnyArgs(0);
        await sut.Run(runSettings, cancel);
        sut.GenericToMutagenSettings.Received(1).Convert(runSettings);
    }

    [Theory, SynthAutoData]
    public async Task PassesConvertedSettingsToFormatter(
        RunSynthesisPatcher runSettings,
        RunSynthesisMutagenPatcher internalRunSettings,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default!, default).ReturnsForAnyArgs(0);
        sut.GenericToMutagenSettings.Convert(default!).ReturnsForAnyArgs(internalRunSettings);
        await sut.Run(runSettings, cancel);
        sut.Format.Received(1).Format(internalRunSettings);
    }

    [Theory, SynthAutoData]
    public async Task ArgsPassedToRunner(
        RunSynthesisPatcher runSettings,
        string args,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default!, default).ReturnsForAnyArgs(0);
        sut.Format.Format<RunSynthesisMutagenPatcher>(default!).ReturnsForAnyArgs(args);
        await sut.Run(runSettings, cancel);
        await sut.ProcessRunner.Received(1).Run(
            Arg.Is<ProcessStartInfo>(x => x.Arguments == args),
            Arg.Any<CancellationToken>());
    }

    [Theory, SynthAutoData]
    public async Task RunnerWorkingDirectorySet(
        RunSynthesisPatcher runSettings,
        FilePath exePath,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ExePath.Path.Returns(exePath);
        sut.ProcessRunner.Run(default!, default!, default).ReturnsForAnyArgs(0);
        await sut.Run(runSettings, cancel);
        await sut.ProcessRunner.Received(1).Run(
            Arg.Is<ProcessStartInfo>(x => x.WorkingDirectory == exePath.Directory!.Value.Path),
            Arg.Any<CancellationToken>());
    }

    [Theory, SynthAutoData]
    public async Task RunnerPassedCancel(
        RunSynthesisPatcher runSettings,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default!, default).ReturnsForAnyArgs(0);
        await sut.Run(runSettings, cancel);
        await sut.ProcessRunner.Received(1).Run(
            Arg.Any<ProcessStartInfo>(),
            cancel);
    }

    [Theory, SynthAutoData]
    public async Task RunnerPassedLoggerCallbacks(
        RunSynthesisPatcher runSettings,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default!, default).ReturnsForAnyArgs(0);
        await sut.Run(runSettings, cancel);
        await sut.ProcessRunner.Received(1).Run(
            Arg.Any<ProcessStartInfo>(),
            Arg.Any<CancellationToken>());
    }
        
    [Theory, SynthAutoData]
    public async Task ProcessThrowsWin(
        IProcessWrapper process,
        RunSynthesisPatcher runSettings,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default!, default)
            .ThrowsForAnyArgs<Win32Exception>();
        process.Run().ThrowsForAnyArgs(_ => new Win32Exception());
        await Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await sut.Run(runSettings, cancel);
        });
    }

    [Theory, SynthAutoData]
    public async Task BadResultThrows(
        RunSynthesisPatcher runSettings,
        CancellationToken cancel,
        CliPatcherRun sut)
    {
        await Assert.ThrowsAsync<CliUnsuccessfulRunException>(async () =>
        {
            sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(-1);
            await sut.Run(runSettings, cancel);
        });
    }

    [Theory, SynthAutoData]
    public async Task CancelledDoesNotThrow(
        RunSynthesisPatcher runSettings,
        CancellationToken cancelled,
        CliPatcherRun sut)
    {
        sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(0);
        await sut.Run(runSettings, cancelled);
    }
}