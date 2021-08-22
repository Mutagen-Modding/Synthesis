using System.IO;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Migration
{
    public class PipelineSettingsVersionRetrieverTests
    {
        [Fact]
        public void Version1Detection()
        {
            new PipelineSettingsVersionRetriever(IFileSystemExt.DefaultFilesystem)
                .GetVersion("Migration/PipelineSettings.json")
                .Should().Be(1);
        }
        
        [Fact]
        public void Version2Detection()
        {
            new PipelineSettingsVersionRetriever(IFileSystemExt.DefaultFilesystem)
                .GetVersion("Migration/PipelineSettings.v2.json")
                .Should().Be(2);
        }
    }
}