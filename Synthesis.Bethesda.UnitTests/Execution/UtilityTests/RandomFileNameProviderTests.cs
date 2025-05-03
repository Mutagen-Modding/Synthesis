using Shouldly;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.UtilityTests;

public class RandomFileNameProviderTests
{
    [Theory, SynthAutoData]
    public void NotTheSameTwice(
        RandomFileNameProvider sut)
    {
        sut.Get().ShouldNotBe(sut.Get());
    }
}