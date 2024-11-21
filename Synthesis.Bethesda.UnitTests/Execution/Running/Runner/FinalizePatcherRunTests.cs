using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class FinalizePatcherRunTests
{
    [Theory, SynthAutoData]
    public async Task OutputFileMissingReturnsNull(
        IPatcherRun patcher,
        ModPath missingOutput,
        FinalizePatcherRun sut)
    {
        sut.Finalize(patcher, missingOutput)
            .Should().BeNull();
    }
        
    [Theory, SynthAutoData]
    public async Task OutputFileExistsReturnsOutputPath(
        IPatcherRun patcher,
        ModPath existingOutput,
        FinalizePatcherRun sut)
    {
        sut.Finalize(patcher, existingOutput)
            .Should().Be(existingOutput.Path);
    }
}