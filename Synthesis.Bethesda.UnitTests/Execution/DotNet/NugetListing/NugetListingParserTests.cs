using FluentAssertions;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.NugetListing;

public class NugetListingParserTests
{
    [Theory, SynthAutoData]
    public void TypicalNugetParse(
        NugetListingParser sut)
    {
        sut.TryParse(
                "   > Mutagen.Bethesda.Synthesis      0.10.7.0    0.10.7   0.10.8.1",
                out var package,
                out var requested,
                out var resolved,
                out var latest)
            .Should().BeTrue();
        package.Should().Be("Mutagen.Bethesda.Synthesis");
        requested.Should().Be("0.10.7.0");
        resolved.Should().Be("0.10.7");
        latest.Should().Be("0.10.8.1");
    }

    [Theory, SynthAutoData]
    public void DepreciatedNugetParse(
        NugetListingParser sut)
    {
        sut.TryParse(
                "   > Mutagen.Bethesda.Synthesis      0.10.7.0    0.10.7 (D)   0.10.8.1",
                out var package,
                out var requested,
                out var resolved,
                out var latest)
            .Should().BeTrue();
        package.Should().Be("Mutagen.Bethesda.Synthesis");
        requested.Should().Be("0.10.7.0");
        resolved.Should().Be("0.10.7");
        latest.Should().Be("0.10.8.1");
    }

    [Theory, SynthAutoData]
    public void NoArrowDelimiterReturnsFalse(
        NugetListingParser sut)
    {
        sut.TryParse(
                "  Mutagen.Bethesda.Synthesis      0.10.7.0    0.10.7 (D)   0.10.8.1",
                out _,
                out _,
                out _,
                out _)
            .Should().BeFalse();
    }
}