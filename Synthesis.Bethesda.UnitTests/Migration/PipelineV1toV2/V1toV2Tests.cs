using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Migration.PipelineV1toV2;

public class V1toV2Tests
{
    private const string V1FilePath = "C:/PipelineSettings.json";
    private const string V1FilePathBackup = "C:/PipelineSettings.v1.json";
        
    public PipelineSettingsImporter MakeImporter(
        IExtraDataPathProvider extraDataPathProvider,
        out MockFileSystem fileSystem)
    {
        fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
        {
            { V1FilePath, File.ReadAllText(Path.Combine("Migration", "PipelineV1toV2", "PipelineSettings.json")) }
        });
        return new PipelineSettingsImporter(
            new PipelineSettingsV1Reader(fileSystem),
            new PipelineSettingsV2Reader(fileSystem),
            new PipelineSettingsUpgrader(
                new PipelineSettingsV1Upgrader(
                    fileSystem,
                    extraDataPathProvider)),
            new PipelineSettingsBackup(fileSystem),
            new SettingsVersionRetriever(fileSystem));
    }

    [Theory, SynthAutoData]
    public void Upgrade(IExtraDataPathProvider extraDataPathProvider)
    {
        var import = MakeImporter(extraDataPathProvider, out var mockFileSystem)
            .Import(V1FilePath);
        mockFileSystem.File.WriteAllText("C:/Test", JsonConvert.SerializeObject(import, Formatting.Indented, Synthesis.Bethesda.Execution.Constants.JsonSettings));
        var expected = IFileSystemExt.DefaultFilesystem.File.ReadAllText(Path.Combine("Migration", "PipelineV1toV2", "PipelineSettings.v2.json"));
        var reimport = mockFileSystem.File.ReadAllText("C:/Test");
        reimport.Should().Be(expected);
    }
        
    [Theory, SynthAutoData]
    public void MakesV1Backup(IExtraDataPathProvider extraDataPathProvider)
    {
        MakeImporter(extraDataPathProvider, out var mockFileSystem)
            .Import(V1FilePath);
        mockFileSystem.File.Exists(V1FilePathBackup)
            .Should().BeTrue();
        mockFileSystem.File.ReadAllText(V1FilePath).Should().Be(
            mockFileSystem.File.ReadAllText(V1FilePathBackup));
    }
        
    [Theory, SynthAutoData]
    public void MovesUserData(
        IExtraDataPathProvider extraDataPathProvider)
    {
        var patcherNames = new string[]
        {
            "Patcher1",
            "Patcher2",
            "Patcher3",
        };
        var importer = MakeImporter(extraDataPathProvider, out var mockFileSystem);
        mockFileSystem.Directory.CreateDirectory(extraDataPathProvider.Path);
        mockFileSystem.Directory.CreateDirectory(Path.Combine(extraDataPathProvider.Path, "Patcher1"));
        mockFileSystem.File.WriteAllText(Path.Combine(extraDataPathProvider.Path, "Patcher1", "settings.json"), string.Empty);
        mockFileSystem.Directory.CreateDirectory(Path.Combine(extraDataPathProvider.Path, "Patcher2"));
        mockFileSystem.File.WriteAllText(Path.Combine(extraDataPathProvider.Path, "Patcher2", "settings.json"), string.Empty);
        mockFileSystem.Directory.CreateDirectory(Path.Combine(extraDataPathProvider.Path, "Patcher3"));
        mockFileSystem.File.WriteAllText(Path.Combine(extraDataPathProvider.Path, "Patcher3", "settings.json"), string.Empty);
        mockFileSystem.Directory.CreateDirectory(Path.Combine(extraDataPathProvider.Path, "Patcher4"));
        mockFileSystem.File.WriteAllText(Path.Combine(extraDataPathProvider.Path, "Patcher4", "settings.json"), string.Empty);
            
        importer.Import(V1FilePath);
            
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Patcher1"))
            .Should().BeFalse();
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Patcher2"))
            .Should().BeFalse();
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Patcher3"))
            .Should().BeFalse();
        foreach (var name in patcherNames)
        {
            mockFileSystem.File.Exists(Path.Combine(extraDataPathProvider.Path, name))
                .Should().BeFalse();
        }
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Profile1", "Patcher1"))
            .Should().BeTrue();
        mockFileSystem.File.Exists(Path.Combine(extraDataPathProvider.Path, "Profile1", "Patcher1", "settings.json"))
            .Should().BeTrue();
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Profile1", "Patcher2"))
            .Should().BeTrue();
        mockFileSystem.File.Exists(Path.Combine(extraDataPathProvider.Path, "Profile1", "Patcher2", "settings.json"))
            .Should().BeTrue();
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Profile2", "Patcher3"))
            .Should().BeTrue();
        mockFileSystem.File.Exists(Path.Combine(extraDataPathProvider.Path, "Profile2", "Patcher3", "settings.json"))
            .Should().BeTrue();
        mockFileSystem.Directory.Exists(Path.Combine(extraDataPathProvider.Path, "Unknown Profile", "Patcher4"))
            .Should().BeTrue();
        mockFileSystem.File.Exists(Path.Combine(extraDataPathProvider.Path, "Unknown Profile", "Patcher4", "settings.json"))
            .Should().BeTrue();
    }
}