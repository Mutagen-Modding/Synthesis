using System.Diagnostics;
using Shouldly;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.IO;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.PatcherCommands;

public class ExecuteOpenForSettingsTests
{
    [Theory, SynthAutoData]
    public async Task PassesListingsToTempLoadOrderProvider(
        string path,
        bool directExe,
        ModKey modKey,
        IEnumerable<IModListingGetter> loadOrder,
        CancellationToken cancel,
        ExecuteOpenForSettings sut)
    {
        await sut.Open(path, directExe, modKey, loadOrder, cancel);
        sut.LoadOrderProvider.Received(1).Get(loadOrder);
    }
        
    [Theory, SynthAutoData]
    public async Task DisposesTemporaryLoadOrder(
        string path,
        bool directExe,
        ModKey modKey,
        IEnumerable<IModListingGetter> loadOrder,
        ITempFile tempFile,
        CancellationToken cancel,
        ExecuteOpenForSettings sut)
    {
        sut.LoadOrderProvider.Get(default!).ReturnsForAnyArgs(tempFile);
        await sut.Open(path, directExe, modKey, loadOrder, cancel);
        tempFile.Received(1).Dispose();
    }

    [Theory, SynthAutoData]
    public async Task StartInfoPassedToRunner(
        string path,
        bool directExe,
        ModKey modKey,
        IEnumerable<IModListingGetter> loadOrder,
        ProcessStartInfo startInfo,
        CancellationToken cancel,
        ExecuteOpenForSettings sut)
    {
        sut.RunProcessStartInfoProvider.GetStart<OpenForSettings>(default!, default, default!)
            .ReturnsForAnyArgs(startInfo);
        await sut.Open(path, directExe, modKey, loadOrder, cancel);
        await sut.ProcessRunner.Received(1).Run(startInfo, cancel);
    }

    [Theory, SynthAutoData]
    public async Task ReturnsRunResult(
        string path,
        bool directExe,
        IEnumerable<IModListingGetter> loadOrder,
        int result,
        ModKey modKey,
        CancellationToken cancel,
        ExecuteOpenForSettings sut)
    {
        sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(result);
        (await sut.Open(path, directExe, modKey, loadOrder, cancel))
            .ShouldBe(result);
    }
}