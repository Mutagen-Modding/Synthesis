using System;
using System.Threading;
using FluentAssertions;
using Noggog;
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
        public void LinesAfterDelimterPassedToParser(
            string line1,
            string line2,
            ProcessNugetQueryResults sut)
        {
            sut.Process(new[] { line1, ProcessNugetQueryResults.Delimeter, line2 });
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
            sut.LineParser.TryParse(line2, out _, out _, out _, out _).Returns(x =>
            {
                x[1] = package;
                x[2] = requested;
                x[3] = resolved;
                x[4] = latest;
                return true;
            });
            sut.Process(new[] { line1, ProcessNugetQueryResults.Delimeter, line2 })
                .Should().Equal(
                    new NugetListingQuery(package, requested, resolved, latest));
        }

        [Theory, SynthAutoData]
        public void LineParserSkipRespected(
            string line1,
            string line2,
            FilePath projPath,
            CancellationToken cancel,
            ProcessNugetQueryResults sut)
        {
            sut.LineParser.TryParse(line2, out _, out _, out _, out _).Returns(
                _ =>
                {
                    return false;
                });
            sut.Process(new[] { line1, ProcessNugetQueryResults.Delimeter, line2 })
                .Should().BeEmpty();
        }
    }
}