using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.Execution.Versioning.Query;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Versioning
{
    public class QueryNewestLibraryVersionsTests
    {
        [Theory, SynthAutoData]
        public async Task PrepsProject(
            CancellationToken cancel,
            QueryNewestLibraryVersions sut)
        {
            await sut.GetLatestVersions(cancel);
            sut.PrepLatestVersionProject.Received(1).Prep();
        }
        
        [Theory, SynthAutoData]
        public async Task PrepThrowsReturnsEmpty(
            CancellationToken cancel,
            QueryNewestLibraryVersions sut)
        {
            sut.PrepLatestVersionProject.When(x => x.Prep())
                .Do(_ => throw new NotImplementedException());
            (await sut.GetLatestVersions(cancel))
                .Should().Be(new NugetVersionOptions(
                    new NugetVersionPair(null, null),
                    new NugetVersionPair(null, null)));
        }
        
        [Theory, SynthAutoData]
        public async Task QueryThrowsReturnsEmpty(
            CancellationToken cancel,
            QueryNewestLibraryVersions sut)
        {
            sut.QueryLibraryVersions.Query(default, default, default, default)
                .ThrowsForAnyArgs<NotImplementedException>();
            (await sut.GetLatestVersions(cancel))
                .Should().Be(new NugetVersionOptions(
                    new NugetVersionPair(null, null),
                    new NugetVersionPair(null, null)));
        }
        
        [Theory, SynthInlineData(false), SynthInlineData(true)]
        public async Task Queries(
            bool includePrerelease,
            FilePath projPath,
            CancellationToken cancel,
            QueryNewestLibraryVersions sut)
        {
            sut.Pathing.ProjectFile.Returns(projPath);
            await sut.GetLatestVersions(cancel);
            await sut.QueryLibraryVersions.Received(1).Query(
                projPath,
                current: false,
                includePrerelease: includePrerelease,
                cancel);
        }
        
        [Theory, SynthAutoData]
        public async Task ReturnsBothQueries(
            NugetVersionPair normal,
            NugetVersionPair prerelease,
            CancellationToken cancel,
            QueryNewestLibraryVersions sut)
        {
            sut.QueryLibraryVersions.Query(
                    Arg.Any<FilePath>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(x.ArgAt<bool>(2) ? prerelease : normal));
            var result = await sut.GetLatestVersions(cancel);
            result.Should().Be(new NugetVersionOptions(
                normal,
                prerelease));
        }
    }
}