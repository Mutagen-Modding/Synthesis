using FluentAssertions;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git;

public class ConstructNameTests
{
    [Theory, SynthAutoData]
    public void EmptyPath(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct(string.Empty)
            .Should().Be(ConstructNameFromRepositoryPath.FallbackName);
    }
        
    [Theory, SynthAutoData]
    public void TrimsToLastForwardSlash(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("Some/Path/Name")
            .Should().Be("Name");
    }
        
    [Theory, SynthAutoData]
    public void TrimsOutTrailingSlash(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("Some/Path/Name/")
            .Should().Be("Name");
    }
        
    [Theory, SynthAutoData]
    public void TrimsOutTrailingSlashes(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("Some/Path/Name///")
            .Should().Be("Name");
    }
        
    [Theory, SynthAutoData]
    public void NoTrim(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("SomeName")
            .Should().Be("SomeName");
    }
}