using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.GUI.Json;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Migration.GuiV1toV2;

public class V1toV2Tests
{
    [Theory, SynthAutoData]
    public void Upgrade(
        MockFileSystem fs,
        IGuiSettingsPath settingsPath)
    {
        fs.File.WriteAllText(settingsPath.Path, File.ReadAllText(Path.Combine("Migration", "GuiV1toV2", "GuiSettings.json")));
        var import = new SettingsSingleton(
                fs,
                settingsPath,
                new GuiSettingsImporter(fs),
                Substitute.For<IPipelineSettingsImporter>(),
                new PipelineSettingsMigration(fs, new SettingsVersionRetriever(fs)),
                new PipelineSettingsPath());
        import.Pipeline.BuildCorePercentage.Should().Be(.123);
        import.Pipeline.WorkingDirectory.Should().Be("Testing123");
        import.Pipeline.DotNetPathOverride.Should().Be("HelloWorld");
    }
}