using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.NugetListing;

public class QueryNugetListingTests
{
    [Theory, SynthAutoData]
    public async Task CallsConstructListWithProjPath(
        FilePath projPath,
        CancellationToken cancel,
        QueryNugetListing sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
        await sut.Query(projPath, default, default, cancel);
        sut.NetCommandStartConstructor.Received(1).Construct("list", projPath, Arg.Any<string[]>());
    }
        
    [Theory, SynthInlineData(true), SynthInlineData(false)]
    public async Task ConstructListRespectsOutdated(
        bool outdated,
        FilePath projPath,
        CancellationToken cancel,
        QueryNugetListing sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
        string[]? passedArgs = null;
        sut.NetCommandStartConstructor.Construct(Arg.Any<string>(), Arg.Any<FilePath>(),
            Arg.Do<string[]>(x => passedArgs = x));
        await sut.Query(projPath, outdated: outdated, default, cancel);
        if (outdated)
        {
            passedArgs.Should().Contain("--outdated");
        }
        else
        {
            passedArgs.Should().NotContain("--outdated");
        }
    }
        
    [Theory, SynthInlineData(true), SynthInlineData(false)]
    public async Task ConstructListRespectsIncludePrerelease(
        bool inclPrerelease,
        FilePath projPath,
        CancellationToken cancel,
        QueryNugetListing sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
        string[]? passedArgs = null;
        sut.NetCommandStartConstructor.Construct(Arg.Any<string>(), Arg.Any<FilePath>(),
            Arg.Do<string[]>(x => passedArgs = x));
        await sut.Query(projPath, default, includePrerelease: inclPrerelease, cancel);
        if (inclPrerelease)
        {
            passedArgs.Should().Contain("--include-prerelease");
        }
        else
        {
            passedArgs.Should().NotContain("--include-prerelease");
        }
    }

    [Theory, SynthAutoData]
    public async Task ThrowsIfAnyErrorsReported(
        FilePath projPath,
        CancellationToken cancel,
        QueryNugetListing sut)
    {
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
            new ProcessRunReturn(0, new(), new() { "Err" }));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await sut.Query(projPath, default, default, cancel);
        });
    }

    [Theory, SynthAutoData]
    public async Task PassesResultsToProcessor(
        FilePath projPath,
        CancellationToken cancel,
        List<string> processOutput,
        IEnumerable<NugetListingQuery> functionReturn,
        QueryNugetListing sut)
    {
        var processReturn = new ProcessRunReturn(0, processOutput, new List<string>());
        sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(processReturn);
        sut.ResultProcessor.Process(processReturn.Out).Returns(functionReturn);
        var result = await sut.Query(projPath, default, default, cancel);
        result.Should().Equal(functionReturn);
    }
}