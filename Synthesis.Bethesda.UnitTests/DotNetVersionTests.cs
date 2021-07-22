using FluentAssertions;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class DotNetVersionTests
    {
        [Theory]
        [SynthInlineData("5.0.1", true)]
        [SynthInlineData("6.0.100-preview.2.21119.3", true)]
        [SynthInlineData("x.5.6", false)]
        public void TestVersionString(
            string str,
            bool shouldSucceed,
            QueryInstalledSdk sut)
        {
            sut.ParseVersionString(str)
                .Acceptable.Should().Be(shouldSucceed);
        }
    }
}
