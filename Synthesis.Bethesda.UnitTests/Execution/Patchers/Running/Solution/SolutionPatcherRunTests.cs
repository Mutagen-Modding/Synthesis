using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution;

public class SolutionPatcherRunTests
{
    [Theory, SynthAutoData]
    public async Task PrepCallsService(
        CancellationToken cancel,
        SolutionPatcherRun sut)
    {
        await sut.Prep(cancel);
        await sut.PrepService.Received(1).Prep(cancel);
    }

    [Theory, SynthAutoData]
    public async Task ToStringCallsNameProvider(
        SolutionPatcherRun sut)
    {
        sut.NameProvider.ClearReceivedCalls();
        sut.ToString();
        var n = sut.NameProvider.Received(1).Name;
    }

    [Theory, SynthAutoData]
    public async Task PrintsSha(
        RunSynthesisPatcher settings,
        CancellationToken cancel,
        SolutionPatcherRun sut)
    {
        await sut.Run(settings, cancel);
        sut.PrintShaIfApplicable.Received(1).Print();
    }
        
    [Theory, SynthAutoData]
    public async Task PassesSettingsToRunner(
        RunSynthesisPatcher settings,
        CancellationToken cancel,
        SolutionPatcherRun sut)
    {
        await sut.Run(settings, cancel);
        await sut.SolutionPatcherRunner.Received(1).Run(settings, cancel);
    }
}