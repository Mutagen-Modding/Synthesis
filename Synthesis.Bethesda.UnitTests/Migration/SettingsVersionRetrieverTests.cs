using System.IO;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Migration;

public class SettingsVersionRetrieverTests
{
    [Fact]
    public void NoVersionDetection()
    {
        new SettingsVersionRetriever(IFileSystemExt.DefaultFilesystem)
            .GetVersion(Path.Combine("Migration", "PipelineV1toV2", "PipelineSettings.json"))
            .Should().BeNull();
    }
        
    [Fact]
    public void Version2Detection()
    {
        new SettingsVersionRetriever(IFileSystemExt.DefaultFilesystem)
            .GetVersion(Path.Combine("Migration", "PipelineV1toV2", "PipelineSettings.v2.json"))
            .Should().Be(2);
    }
}