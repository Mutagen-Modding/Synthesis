using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Migration;

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
        mockFileSystem.File.ReadAllText("C:\\Path.v1.json").Should().Be("Test1");
            
        mockFileSystem.File.WriteAllText(path, "Test2");
        sut.Backup(1, path);
        mockFileSystem.File.ReadAllText("C:\\Path.v1.json").Should().Be("Test2");
    }
}