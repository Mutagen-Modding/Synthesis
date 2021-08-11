using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Json.Pipeline
{
    public class PipelineSettingsVersionRetrieverTests
    {
        [Theory, SynthAutoData]
        public void NoVersionReturnsOne(
            MockFileSystem fileSystem,
            FilePath path,
            PipelineSettingsVersionRetriever sut)
        {
            fileSystem.File.WriteAllText(path, "{}");
            sut.GetVersion(path).Should().Be(1);
        }
        
        [Theory, SynthAutoData]
        public void ParsesVersion(
            MockFileSystem fileSystem,
            FilePath path,
            PipelineSettingsVersionRetriever sut)
        {
            fileSystem.File.WriteAllText(path, "{ \"Version\": \"3\" }");
            sut.GetVersion(path).Should().Be(3);
        }
    }
}