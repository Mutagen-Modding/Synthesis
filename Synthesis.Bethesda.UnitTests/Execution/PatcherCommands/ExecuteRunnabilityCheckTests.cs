using System.Diagnostics;
using AutoFixture.Xunit2;
using Shouldly;
using Mutagen.Bethesda.Plugins;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.PatcherCommands;

public class CheckRunnabilityTests
{
    [Theory, SynthAutoData]
    public async Task DoesNotHaveRunnabilityMetaShortCircuits(
        string path,
        bool directExe,
        string loadOrderPath,
        ModKey modKey,
        FilePath buildMetaPath,
        CancellationToken cancel,
        GitCompilationMeta compilationMeta,
        [Frozen] IShortCircuitSettingsProvider shortCircuitSettingsProvider,
        ExecuteRunnabilityCheck sut)
    {
        shortCircuitSettingsProvider.Shortcircuit.Returns(true);
        sut.MetaFileReader.Read(buildMetaPath).Returns(compilationMeta with { DoesNotHaveRunnability = true });
        await sut.Check(modKey, path, directExe, loadOrderPath, buildMetaPath, cancel);
        await sut.ProcessRunner.DidNotReceiveWithAnyArgs().RunWithCallback(default!, default(Action<string>)!, default);
    }
        
    [Theory, SynthAutoData]
    public async Task PassesParametersToGetStart(
        string path,
        bool directExe,
        string loadOrderPath,
        ModKey modKey,
        FilePath buildMetaPath,
        CancellationToken cancel,
        ExecuteRunnabilityCheck sut)
    {
        await sut.Check(modKey, path, directExe, loadOrderPath, buildMetaPath, cancel);
        sut.RunProcessStartInfoProvider.Received(1).GetStart(path, directExe, new CheckRunnability()
        {
            DataFolderPath = sut.DataDirectoryProvider.Path,
            GameRelease = sut.GameReleaseContext.Release,
            LoadOrderFilePath = loadOrderPath,
            ExtraDataFolder = sut.ExtraDataPathProvider.Path,
            ModKey = modKey.ToString()
        });
    }

    [Theory, SynthAutoData]
    public async Task GetStartPassesToRunner(
        string path,
        bool directExe,
        string loadOrderPath,
        ModKey modKey,
        FilePath buildMetaPath,
        CancellationToken cancel,
        ProcessStartInfo start,
        ExecuteRunnabilityCheck sut)
    {
        sut.RunProcessStartInfoProvider.GetStart<CheckRunnability>(default!, default, default!)
            .ReturnsForAnyArgs(start);
        await sut.Check(modKey, path, directExe, loadOrderPath, buildMetaPath, cancel);
        await sut.ProcessRunner.Received(1).RunWithCallback(
            start,
            Arg.Any<Action<string>>(),
            cancel);
    }

    [Theory, SynthAutoData]
    public async Task NotRunnableResponseFails(
        string path,
        bool directExe,
        string loadOrderPath,
        ModKey modKey,
        FilePath buildMetaPath,
        CancellationToken cancel,
        ExecuteRunnabilityCheck sut)
    {
        sut.ProcessRunner.RunWithCallback(default!, default!, default)
            .ReturnsForAnyArgs((int)Codes.NotRunnable);
        (await sut.Check(modKey, path, directExe, loadOrderPath, buildMetaPath, cancel))
            .Succeeded.ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public async Task FailedPrintReturnsOnlyMaxLines(
        string path,
        bool directExe,
        string loadOrderPath,
        ModKey modKey,
        FilePath buildMetaPath,
        CancellationToken cancel,
        ExecuteRunnabilityCheck sut)
    {
        sut.ProcessRunner.RunWithCallback(
                Arg.Any<ProcessStartInfo>(),
                Arg.Do<Action<string>>(x =>
                {
                    for (int i = 0; i < ExecuteRunnabilityCheck.MaxLines + 5; i++)
                    {
                        x("line");
                    }
                }),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs((int)Codes.NotRunnable);
        var resp = await sut.Check(modKey, path, directExe, loadOrderPath, buildMetaPath, cancel);
        resp.Reason.Split(Environment.NewLine).Length.ShouldBe(ExecuteRunnabilityCheck.MaxLines);
    }

    [Theory, SynthAutoData]
    public async Task AnyOtherResponseSucceeds(
        string path,
        bool directExe,
        string loadOrderPath,
        ModKey modKey,
        FilePath buildMetaPath,
        CancellationToken cancel,
        ExecuteRunnabilityCheck sut)
    {
        sut.ProcessRunner.RunWithCallback(default!, default!, default)
            .ReturnsForAnyArgs(((int)Codes.NotRunnable) + 1);
        (await sut.Check(modKey, path, directExe, loadOrderPath, buildMetaPath, cancel))
            .Succeeded.ShouldBeTrue();
    }
}