using System.IO.Abstractions;
using Shouldly;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.GUI.Json;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Migration.GuiV1toV2;

public class V1toV2Tests
{
    [Theory, SynthAutoData]
    public void Upgrade(
        IFileSystem fs,
        IPipelineSettingsPath pipelineSettingsPath,
        IGuiSettingsPath settingsPath)
    {
        fs.File.WriteAllText(settingsPath.Path, File.ReadAllText(Path.Combine("Migration", "GuiV1toV2", "GuiSettings.json")));
        var import = new SettingsSingleton(
                fs,
                settingsPath,
                new GuiSettingsImporter(fs),
                Substitute.For<IPipelineSettingsImporter>(),
                new PipelineSettingsMigration(fs, new SettingsVersionRetriever(fs)),
                pipelineSettingsPath);
        import.Pipeline.BuildCorePercentage.ShouldBe(.123);
        import.Pipeline.WorkingDirectory.ShouldBe("Testing123");
        import.Pipeline.DotNetPathOverride.ShouldBe("HelloWorld");
    }
}