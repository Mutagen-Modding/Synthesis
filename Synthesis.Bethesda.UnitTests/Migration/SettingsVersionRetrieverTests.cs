using Shouldly;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json;

namespace Synthesis.Bethesda.UnitTests.Migration;

public class SettingsVersionRetrieverTests
{
    [Fact]
    public void NoVersionDetection()
    {
        new SettingsVersionRetriever(IFileSystemExt.DefaultFilesystem)
            .GetVersion(Path.Combine("Migration", "PipelineV1toV2", "PipelineSettings.json"))
            .ShouldBeNull();
    }
        
    [Fact]
    public void Version2Detection()
    {
        new SettingsVersionRetriever(IFileSystemExt.DefaultFilesystem)
            .GetVersion(Path.Combine("Migration", "PipelineV1toV2", "PipelineSettings.v2.json"))
            .ShouldBe(2);
    }
}