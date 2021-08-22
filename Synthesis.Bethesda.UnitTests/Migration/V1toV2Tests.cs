using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Migration
{
    public class V1toV2Tests
    {
        private const string V1FilePath = "C:/PipelineSettings.json";
        private const string V1FilePathBackup = "C:/PipelineSettings.v1.json";
        
        public PipelineSettingsImporter MakeImporter(out MockFileSystem fileSystem)
        {
            fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
            {
                { V1FilePath, File.ReadAllText("Migration/PipelineSettings.json") }
            });
            return new PipelineSettingsImporter(
                new PipelineSettingsV1Reader(fileSystem),
                new PipelineSettingsV2Reader(fileSystem),
                new PipelineSettingsUpgrader(
                    new PipelineSettingsV1Upgrader()),
                new PipelineSettingsBackup(fileSystem),
                new PipelineSettingsVersionRetriever(fileSystem));
        }

        [Fact]
        public void Upgrade()
        {
            var import = MakeImporter(out var mockFileSystem)
                .Import(V1FilePath);
            mockFileSystem.File.WriteAllText("C:/Test", JsonConvert.SerializeObject(import, Formatting.Indented, Synthesis.Bethesda.Execution.Constants.JsonSettings));
            var expected = IFileSystemExt.DefaultFilesystem.File.ReadAllText("Migration/PipelineSettings.v2.json");
            var reimport = mockFileSystem.File.ReadAllText("C:/Test");
            reimport.Should().Be(expected);
        }
        
        [Fact]
        public void MakesV1Backup()
        {
            MakeImporter(out var mockFileSystem)
                .Import(V1FilePath);
            mockFileSystem.File.Exists(V1FilePathBackup)
                .Should().BeTrue();
            mockFileSystem.File.ReadAllText(V1FilePath).Should().Be(
                mockFileSystem.File.ReadAllText(V1FilePathBackup));
        }
    }
}