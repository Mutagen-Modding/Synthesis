using System;
using System.Threading.Tasks;
using FluentAssertions;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class FinalizePatcherRunTests
    {
        [Theory, SynthAutoData]
        public async Task OutputFileMissingReturnsNull(
            Guid key,
            IPatcherRun patcher,
            ModPath missingOutput,
            FinalizePatcherRun sut)
        {
            sut.Finalize(patcher, key, missingOutput)
                .Should().BeNull();
        }
        
        [Theory, SynthAutoData]
        public async Task OutputFileExistsReturnsOutputPath(
            Guid key,
            IPatcherRun patcher,
            ModPath existingOutput,
            FinalizePatcherRun sut)
        {
            sut.Finalize(patcher, key, existingOutput)
                .Should().Be(existingOutput.Path);
        }
    }
}