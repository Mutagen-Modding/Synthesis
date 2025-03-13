using Shouldly;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.ExecutablePath;

public class RetrieveExecutablePathTests
{
    [Theory, SynthAutoData]
    public void FailedLiftReturnsFalse(
        FilePath path,
        IEnumerable<string> lines,
        RetrieveExecutablePath sut)
    {
        sut.Lift.TryGet(default!, out _).ReturnsForAnyArgs(false);
        sut.TryGet(path, lines, out _)
            .ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void PassesLinesToLift(
        FilePath path,
        IEnumerable<string> lines,
        RetrieveExecutablePath sut)
    {
        sut.TryGet(path, lines, out _);
        sut.Lift.Received(1).TryGet(lines, out Arg.Any<string?>());
    }
        
    [Theory, SynthAutoData]
    public void LiftPassesToProcess(
        string liftRet,
        FilePath path,
        IEnumerable<string> lines,
        RetrieveExecutablePath sut)
    {
        sut.Lift.TryGet(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = liftRet;
            return true;
        });
        sut.TryGet(path, lines, out _);
        sut.Process.Received(1).Process(path, liftRet);
    }

    [Theory, SynthAutoData]
    public void ProcessReturns(
        string processRet,
        FilePath path,
        IEnumerable<string> lines,
        RetrieveExecutablePath sut)
    {
        sut.Process.Process(default!, default!).ReturnsForAnyArgs(processRet);
        sut.Lift.TryGet(default!, out _).ReturnsForAnyArgs(x =>
        {
            x[1] = string.Empty;
            return true;
        });
        sut.TryGet(path, lines, out var output)
            .ShouldBeTrue();
        output.ShouldBe(processRet);
    }
}