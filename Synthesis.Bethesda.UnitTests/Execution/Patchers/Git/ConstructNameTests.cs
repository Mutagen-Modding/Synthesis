using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git;

public class ConstructNameTests
{
    [Theory, SynthAutoData]
    public void EmptyPath(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct(string.Empty)
            .ShouldBe(ConstructNameFromRepositoryPath.FallbackName);
    }
        
    [Theory, SynthAutoData]
    public void TrimsToLastForwardSlash(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("Some/Path/Name")
            .ShouldBe("Name");
    }
        
    [Theory, SynthAutoData]
    public void TrimsOutTrailingSlash(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("Some/Path/Name/")
            .ShouldBe("Name");
    }
        
    [Theory, SynthAutoData]
    public void TrimsOutTrailingSlashes(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("Some/Path/Name///")
            .ShouldBe("Name");
    }
        
    [Theory, SynthAutoData]
    public void NoTrim(ConstructNameFromRepositoryPath sut)
    {
        sut.Construct("SomeName")
            .ShouldBe("SomeName");
    }
}