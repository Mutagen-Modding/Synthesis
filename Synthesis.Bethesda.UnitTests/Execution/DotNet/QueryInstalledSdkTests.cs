using System.Diagnostics;
using Shouldly;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet;

public class QueryInstalledSdkTests
{
    [Theory, SynthAutoData]
    public async Task PassesCommandPathProviderToStartInfo(
        FilePath path,
        CancellationToken cancel,
        QueryInstalledSdk sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new() { "Something" }, new()));
        sut.NetCommandPathProvider.Path.Returns(path.Path);
        await sut.Query(cancel);
        await sut.ProcessRunner.Received(1).RunAndCapture(
            Arg.Is<ProcessStartInfo>(x => x.FileName.Equals(path.Path, StringComparison.OrdinalIgnoreCase)),
            cancel);
    }

    [Theory, SynthAutoData]
    public async Task ThrowsIfAnyErrors(
        CancellationToken cancel,
        QueryInstalledSdk sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new(), new() {"Error"}));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Query(cancel));
    }

    [Theory, SynthAutoData]
    public async Task ThrowsIfOutIsManyLines(
        CancellationToken cancel,
        QueryInstalledSdk sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new(){ "Out", "Many" }, new()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Query(cancel));
    }

    [Theory, SynthAutoData]
    public async Task ThrowsIfOutIsNoLines(
        CancellationToken cancel,
        QueryInstalledSdk sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn());
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Query(cancel));
    }

    [Theory, SynthAutoData]
    public async Task PassesFirstResultToParse(
        string str,
        CancellationToken cancel,
        QueryInstalledSdk sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new(){ str }, new()));
        await sut.Query(cancel);
        sut.NugetVersionString.Received(1).Parse(str);
    }

    [Theory, SynthAutoData]
    public async Task QueryReturnsParserResult(
        DotNetVersion vers,
        CancellationToken cancel,
        QueryInstalledSdk sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new() { "Something" }, new()));
        sut.NugetVersionString.Parse(default!).ReturnsForAnyArgs(vers);
        (await sut.Query(cancel))
            .ShouldBeSameAs(vers);
    }
}