using System;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.NugetListing
{
    public class ProcessNugetQueryResultsTests
    {
        [Theory, SynthAutoData]
        public void NoResultsReturnsNoResults(
            ProcessNugetQueryResults sut)
        {
            sut.Process(Array.Empty<string>())
                .Should().BeEmpty();
        }
        
        [Theory, SynthAutoData]
        public void NoTopLevelLineReturnsNoResults(
            string line1,
            string line2,
            ProcessNugetQueryResults sut)
        {
            sut.Process(new[] { line1, line2 })
                .Should().BeEmpty();
        }

        [Theory, SynthAutoData]
        public void LinesIncludedOnlyIfHavePrefix(
            string line1,
            string line2,
            ProcessNugetQueryResults sut)
        {
            line2 = $"{ProcessNugetQueryResults.Prefix}{line2}";
            sut.Process(new[] { line1, line2 });
            sut.LineParser.Received(1).TryParse(line2,
                out Arg.Any<string?>(),
                out Arg.Any<string?>(),
                out Arg.Any<string?>(),
                out Arg.Any<string?>());
        }

        [Theory, SynthAutoData]
        public void ParserResultsGetReturned(
            string line1,
            string line2,
            string package,
            string requested,
            string resolved,
            string latest,
            ProcessNugetQueryResults sut)
        {
            line2 = $"{ProcessNugetQueryResults.Prefix}{line2}";
            sut.LineParser.TryParse(line2, out _, out _, out _, out _).Returns(x =>
            {
                x[1] = package;
                x[2] = requested;
                x[3] = resolved;
                x[4] = latest;
                return true;
            });
            sut.Process(new[] { line1, line2 })
                .Should().Equal(
                    new NugetListingQuery(package, requested, resolved, latest));
        }

        [Theory, SynthAutoData]
        public void LineParserSkipRespected(
            string line1,
            string line2,
            ProcessNugetQueryResults sut)
        {
            line2 = $"{ProcessNugetQueryResults.Prefix}{line2}";
            sut.LineParser.TryParse(line2, out _, out _, out _, out _).Returns(
                _ =>
                {
                    return false;
                });
            sut.Process(new[] { line1,  line2 })
                .Should().BeEmpty();
        }
        
        public static IEnumerable<object[]> IntegrationTestCases => new object[][]
        {
            new object[]
            {
                new List<string>()
                {
                    @"The following sources were used:",
                    @"   https://api.nuget.org/v3/index.json",
                    @"   C:\Repos\CSharpExt\Noggog.CSharpExt\bin\Debug",
                    @"   C:\Repos\CSharpExt\Noggog.WPF\bin\Debug",
                    @"",
                    @"Project `VersionQuery` has the following updates to its packages",
                    @"   [net6.0-windows7.0]:",
                    @"   Top-level Package                 Requested   Resolved   Latest",
                    @"   > Mutagen.Bethesda                0.14.0      0.14.0     0.33.7.1-dev",
                    @"   > Mutagen.Bethesda.Synthesis      0.0.3       0.0.3      0.20.6.1-dev",
                },
                new NugetListingQuery[]
                {
                    new NugetListingQuery("Mutagen.Bethesda", "0.14.0", "0.14.0", "0.33.7.1-dev"),
                    new NugetListingQuery("Mutagen.Bethesda.Synthesis", "0.0.3", "0.0.3", "0.20.6.1-dev"),
                }
            },
            new object[]
            {
                new List<string>()
                {
                    @"Użyto następujących źródeł:",
                    @"   https://api.nuget.org/v3/index.json",
                    @"   C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\",
                    @"",
                    "Projekt \"VersionQuery\" ma następujące aktualizacje jego pakietów",
                    @"   [net5.0-windows7.0]:",
                    @"   Pakiet najwyższego poziomu        Żądane   Rozpoznane   Najnowsze",
                    @"   > Mutagen.Bethesda                0.14.0   0.14.0       0.33.7-pr001",
                    @"   > Mutagen.Bethesda.Synthesis      0.0.3    0.0.3        0.20.6",
                },
                new NugetListingQuery[]
                {
                    new NugetListingQuery("Mutagen.Bethesda", "0.14.0", "0.14.0", "0.33.7-pr001"),
                    new NugetListingQuery("Mutagen.Bethesda.Synthesis", "0.0.3", "0.0.3", "0.20.6"),
                }
            }
        };

        [Theory, SynthMemberData(nameof(IntegrationTestCases))]
        public void IntegrationTests(string[] processOutput, NugetListingQuery[] results)
        {
            new ProcessNugetQueryResults(new NugetListingParser())
                .Process(processOutput).Should().Equal(results);
        }
    }
}