using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.NugetListing
{
    public class QueryNugetListingTests
    {
        [Theory, SynthAutoData]
        public async Task CallsConstructRestoreWithProjPath(
            FilePath projPath,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
            await sut.Query(projPath, default, default, cancel);
            sut.NetCommandStartConstructor.Received(1).Construct("restore", projPath);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesRestoreCommandToRunner(
            FilePath projPath,
            [Frozen]ProcessStartInfo startInfo,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
            sut.NetCommandStartConstructor.Construct("restore", projPath).Returns(startInfo);
            await sut.Query(projPath, default, default, cancel);
            await sut.ProcessRunner.Received(1).Run(startInfo, cancel);
        }
        
        [Theory, SynthAutoData]
        public async Task RestoreFailingDoesNotRunList(
            FilePath projPath,
            [Frozen]ProcessStartInfo startInfo,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(new ProcessRunReturn());
            sut.NetCommandStartConstructor.Construct("restore", projPath).Throws<NotImplementedException>();
            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Query(projPath, default, default, cancel);
            });
            await sut.ProcessRunner.DidNotReceiveWithAnyArgs().RunAndCapture(default!, default);
        }
        
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
        public async Task NoResultsReturnsNoResults(
            FilePath projPath,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn(0, new(), new()));
            (await sut.Query(projPath, default, default, cancel))
                .Should().BeEmpty();
        }

        [Theory, SynthAutoData]
        public async Task NoTopLevelLineReturnsNoResults(
            string line1,
            string line2,
            FilePath projPath,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn(0, new() { line1, line2 }, new()));
            (await sut.Query(projPath, default, default, cancel))
                .Should().BeEmpty();
        }

        [Theory, SynthAutoData]
        public async Task LinesAfterDelimterPassedToParser(
            string line1,
            string line2,
            FilePath projPath,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn(0, new() { line1, QueryNugetListing.Delimeter, line2 }, new()));
            await sut.Query(projPath, default, default, cancel);
            sut.LineParser.Received(1).TryParse(line2,
                out Arg.Any<string?>(),
                out Arg.Any<string?>(),
                out Arg.Any<string?>(),
                out Arg.Any<string?>());
        }

        [Theory, SynthAutoData]
        public async Task ParserResultsGetReturned(
            string line1,
            string line2,
            string package,
            string requested,
            string resolved,
            string latest,
            FilePath projPath,
            CancellationToken cancel,
            QueryNugetListing sut)
        {
            sut.ProcessRunner.RunAndCapture(default!, default).ReturnsForAnyArgs(
                new ProcessRunReturn(0, new() { line1, QueryNugetListing.Delimeter, line2 }, new()));
            sut.LineParser.TryParse(line2, out _, out _, out _, out _).Returns(x =>
            {
                x[1] = package;
                x[2] = requested;
                x[3] = resolved;
                x[4] = latest;
                return true;
            });
            (await sut.Query(projPath, default, default, cancel))
                .Should().Equal(
                    new NugetListingQuery(package, requested, resolved, latest));
        }
    }
}