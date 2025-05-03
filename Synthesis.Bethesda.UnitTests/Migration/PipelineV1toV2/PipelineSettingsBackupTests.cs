using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Migration.PipelineV1toV2;

public class PipelineSettingsBackupTests
{
    [Theory, SynthAutoData]
    public void DoubleBackupReplaces(
        MockFileSystem mockFileSystem,
        PipelineSettingsBackup sut)
    {
        var path = "C:\\Path.json";
        mockFileSystem.File.WriteAllText(path, "Test1");
            
        sut.Backup(1, path);
        mockFileSystem.File.ReadAllText("C:\\Path.v1.json").ShouldBe("Test1");
            
        mockFileSystem.File.WriteAllText(path, "Test2");
        sut.Backup(1, path);
        mockFileSystem.File.ReadAllText("C:\\Path.v1.json").ShouldBe("Test2");
    }
}