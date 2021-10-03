using FluentAssertions;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git
{
    public class ConstructNameTests
    {
        [Theory, SynthAutoData]
        public void EmptyPath(ConstructName sut)
        {
            sut.Construct(string.Empty)
                .Should().Be(ConstructName.FallbackName);
        }
        
        [Theory, SynthAutoData]
        public void TrimsToLastForwardSlash(ConstructName sut)
        {
            sut.Construct("Some/Path/Name")
                .Should().Be("Name");
        }
        
        [Theory, SynthAutoData]
        public void NoTrim(ConstructName sut)
        {
            sut.Construct("SomeName")
                .Should().Be("SomeName");
        }
    }
}