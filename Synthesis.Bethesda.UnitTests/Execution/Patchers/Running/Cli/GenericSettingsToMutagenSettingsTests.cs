using FluentAssertions;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Cli
{
    public class GenericSettingsToMutagenSettingsTests
    {
        [Theory, SynthAutoData]
        public void SetsExtraDataFolder(
            RunSynthesisPatcher settings,
            GenericSettingsToMutagenSettings sut)
        {
            sut.Convert(settings)
                .ExtraDataFolder.Should().Be(sut.ExtraDataPathProvider.Path);
        }
    }
}