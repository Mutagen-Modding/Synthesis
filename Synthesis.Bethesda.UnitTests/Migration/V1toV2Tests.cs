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
        public PipelineSettingsImporter MakeImporter(IFileSystem fs)
        {
            return new PipelineSettingsImporter(
                new PipelineSettingsV1Reader(fs),
                new PipelineSettingsV2Reader(fs),
                new PipelineSettingsUpgrader(
                    new PipelineSettingsV1Upgrader()),
                new PipelineSettingsVersionRetriever(fs));
        }

        [Fact]
        public void Upgrade()
        {
            var import = MakeImporter(IFileSystemExt.DefaultFilesystem)
                .Import("Migration/PipelineSettings.json");
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.File.WriteAllText("C:/Test", JsonConvert.SerializeObject(import, Formatting.Indented, Synthesis.Bethesda.Execution.Constants.JsonSettings));
            var expected = IFileSystemExt.DefaultFilesystem.File.ReadAllText("Migration/PipelineSettings.v2.json");
            var reimport = mockFileSystem.File.ReadAllText("C:/Test");
            reimport.Should().Be(expected);
        }
    }
}