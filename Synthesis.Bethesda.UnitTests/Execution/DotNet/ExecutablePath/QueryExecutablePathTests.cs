using System.Diagnostics;
using AutoFixture.Xunit2;
using Shouldly;
using Noggog;
using Noggog.NSubstitute;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.ExecutablePath;

public class QueryExecutablePathTests
{
    [Theory, SynthAutoData]
    public async Task PassesProjPathToStartProvider(
        FilePath projPath,
        CancellationToken cancel,
        QueryExecutablePath sut)
    {
        await sut.Query(projPath, cancel);
        sut.StartInfoProvider.Received(1).Construct(projPath);
    }
        
    [Theory, SynthAutoData]
    public async Task StartProviderPassesToRunner(
        FilePath projPath,
        CancellationToken cancel,
        [Frozen]ProcessStartInfo startInfo,
        QueryExecutablePath sut)
    {
        sut.StartInfoProvider.Construct(default).ReturnsForAnyArgs(startInfo);
        await sut.Query(projPath, cancel);
        await sut.Runner.Received(1).RunAndCapture(startInfo.ArgIsSame(), cancel);
    }

    [Theory, SynthAutoData]
    public async Task FailIfAnyErrors(
        FilePath projPath,
        CancellationToken cancel,
        QueryExecutablePath sut)
    {
        sut.Runner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new List<string>(), new List<string>() {"Error"}));
        (await sut.Query(projPath, cancel))
            .Succeeded.ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public async Task FailIfRetrieveFails(
        FilePath projPath,
        CancellationToken cancel,
        QueryExecutablePath sut)
    {
        sut.Runner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
        sut.RetrievePath.TryGet(default, default!, out _).ReturnsForAnyArgs(false);
        (await sut.Query(projPath, cancel))
            .Succeeded.ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public async Task ReturnsRetrievalPathIfSuccessful(
        string retPath,
        FilePath projPath,
        CancellationToken cancel,
        QueryExecutablePath sut)
    {
        sut.Runner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
        sut.RetrievePath.TryGet(default, default!, out _).ReturnsForAnyArgs(x =>
        {
            x[2] = retPath;
            return true;
        });
        var ret = await sut.Query(projPath, cancel);
        ret.Succeeded.ShouldBeTrue();
        ret.Value.ShouldBe(retPath);
    }
}