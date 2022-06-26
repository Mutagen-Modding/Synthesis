using FluentAssertions;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet;

public class ParseNugetVersionStringTests
{
    [Theory]
    [SynthInlineData("5.0.1", true)]
    [SynthInlineData("6.0.100-preview.2.21119.3", true)]
    [SynthInlineData("x.5.6", false)]
    public void TestVersionString(
        string str,
        bool shouldSucceed,
        ParseNugetVersionString sut)
    {
        sut.Parse(str)
            .Acceptable.Should().Be(shouldSucceed);
    }
}