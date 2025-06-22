using Noggog.Testing.Extensions;
using Shouldly;
using Synthesis.Bethesda.Execution.DotNet.Builder.Transient;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.Builder;

public class BuildOutputAccumulatorTests
{
    [Theory, SynthAutoData]
    public void IfBuildFailStringEncounteredFail(
        BuildOutputAccumulator sut)
    {
        sut.Process(BuildOutputAccumulator.BuildFailedString);
        sut.BuildFailed.ShouldBeTrue();
    }
        
    [Theory, SynthAutoData]
    public void BuildFailTriggerShouldNotSetFirstError(
        BuildOutputAccumulator sut)
    {
        sut.Process(BuildOutputAccumulator.BuildFailedString);
        sut.FirstError.ShouldBeNull();
    }
        
    [Theory, SynthAutoData]
    public void NormalLineDoesNotTriggerError(
        BuildOutputAccumulator sut)
    {
        sut.Process("Normal");
        sut.FirstError.ShouldBeNull();
        sut.BuildFailed.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void ErrorTextBeforeFailedTriggerSetsFirstLine(
        BuildOutputAccumulator sut)
    {
        sut.Process("error");
        sut.FirstError.ShouldBeNull();
    }
        
    [Theory, SynthAutoData]
    public void ErrorTextAfterFailedTriggerSetsFirstLine(
        BuildOutputAccumulator sut)
    {
        sut.Process(BuildOutputAccumulator.BuildFailedString);
        sut.Process("error 123");
        sut.FirstError.ShouldBe("error 123");
    }
        
    [Theory, SynthAutoData]
    public void LineAddsToOutput(
        BuildOutputAccumulator sut)
    {
        sut.Process("Normal");
        sut.Output.ShouldEqualEnumerable("Normal");
    }
        
    [Theory, SynthAutoData]
    public void ErrorAddsToOutput(
        BuildOutputAccumulator sut)
    {
        sut.Process("error 123");
        sut.Output.ShouldEqualEnumerable("error 123");
    }
        
    [Theory, SynthAutoData]
    public void StopsAddingAfterLimit(
        BuildOutputAccumulator sut)
    {
        sut.Limit = 5;
        sut.Process("abcd");
        sut.Process("efgh");
        sut.Process("ijkl");
        sut.Output.ShouldEqualEnumerable(
            "abcd",
            "efgh");
    }
}