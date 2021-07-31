using FluentAssertions;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.UtilityTests
{
    public class RandomFileNameProviderTests
    {
        [Theory, SynthAutoData]
        public void NotTheSameTwice(
            RandomFileNameProvider sut)
        {
            sut.Get().Should().NotBe(sut.Get());
        }
    }
}