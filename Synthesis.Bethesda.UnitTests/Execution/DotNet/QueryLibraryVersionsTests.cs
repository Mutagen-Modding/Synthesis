using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet
{
    public class QueryLibraryVersionsTests
    {
        private IQueryNugetListing GetNugetQuery()
        {
            var queryNuget = Substitute.For<IQueryNugetListing>();
            queryNuget.Query(default, default, default, default)
                .ReturnsForAnyArgs(_ => new List<NugetListingQuery>()
                {
                    new NugetListingQuery("Mutagen.Bethesda", Requested: "0.14.0", Resolved: "0.14.0", Latest: "0.30.3"),
                    new NugetListingQuery("Mutagen.Bethesda.Synthesis", Requested: "0.0.3", Resolved: "0.0.3", Latest: "0.19.1"),
                    new NugetListingQuery("Unknown", Requested: "1.2.3", Resolved: "1.2.3", Latest: "4.5.6"),
                });
            return queryNuget;
        }
        
        [Fact]
        public async Task Current()
        {
            var queryNuget = GetNugetQuery();
            var queryLibs = new QueryLibraryVersions(queryNuget);
            var libVersions = await queryLibs.Query(default, true, false, CancellationToken.None);
            libVersions.Mutagen.Should().Be("0.14.0");
            libVersions.Synthesis.Should().Be("0.0.3");
        }
        
        [Fact]
        public async Task Latest()
        {
            var queryNuget = GetNugetQuery();
            var queryLibs = new QueryLibraryVersions(queryNuget);
            var libVersions = await queryLibs.Query(default, false, false, CancellationToken.None);
            libVersions.Mutagen.Should().Be("0.30.3");
            libVersions.Synthesis.Should().Be("0.19.1");
        }
        
        [Fact]
        public async Task MissingMutagen()
        {
            var queryNuget = Substitute.For<IQueryNugetListing>();
            queryNuget.Query(default, default, default, default)
                .ReturnsForAnyArgs(x => new List<NugetListingQuery>()
                {
                    new NugetListingQuery("Mutagen.Bethesda.Synthesis", Requested: "0.0.3", Resolved: "0.0.3", Latest: "0.19.1"),
                    new NugetListingQuery("Unknown", Requested: "1.2.3", Resolved: "1.2.3", Latest: "4.5.6"),
                });
            var queryLibs = new QueryLibraryVersions(queryNuget);
            var libVersions = await queryLibs.Query(string.Empty, false, false, CancellationToken.None);
            libVersions.Mutagen.Should().BeNull();
            libVersions.Synthesis.Should().Be("0.19.1");
        }
        
        [Fact]
        public async Task MissingSynthesis()
        {
            var queryNuget = Substitute.For<IQueryNugetListing>();
            queryNuget.Query(default, default, default, default)
                .ReturnsForAnyArgs(x => new List<NugetListingQuery>()
                {
                    new NugetListingQuery("Mutagen.Bethesda", Requested: "0.14.0", Resolved: "0.14.0", Latest: "0.30.3"),
                    new NugetListingQuery("Unknown", Requested: "1.2.3", Resolved: "1.2.3", Latest: "4.5.6"),
                });
            var queryLibs = new QueryLibraryVersions(queryNuget);
            var libVersions = await queryLibs.Query(string.Empty, false, false, CancellationToken.None);
            libVersions.Mutagen.Should().Be("0.30.3");
            libVersions.Synthesis.Should().BeNull();
        }
    }
}