using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.CLI;
using Shouldly;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Integration test for building and running a patcher ad-hoc via command line
/// </summary>
public class BasicSourceCodePatcherAdhocRunTest : IntegrationTest
{
    public BasicSourceCodePatcherAdhocRunTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task BuildAndRunPatcher_ProducesValidOutput()
    {
        // Arrange

        // Create a test mod in the load order
        var testModKey = ModKey.FromNameAndExtension("TestMod.esp");
        CreateSimpleSkyrimMod(testModKey, mod =>
        {
            var npc = mod.Npcs.AddNew();
            npc.Name = "Integration Test NPC";
        });
        AddToLoadOrder(testModKey, enabled: true);

        // Create a test patcher project
        var projectFolder = CreateTestPatcherProject("TestPatcher", AddTypicalPatcherNpc());

        // Build the patcher
        var buildResult = await BuildPatcher(projectFolder);
        buildResult.ExitCode.ShouldBe(0, $"Build failed:\n{buildResult.Output}");

        // Prepare output
        var outputModKey = ModKey.FromNameAndExtension("SynthesisPatch.esp");
        var outputPath = Path.Combine(TestFolder, "Output");
        Directory.CreateDirectory(outputPath);
        var outputFile = Path.Combine(outputPath, outputModKey.FileName);

        // Act - Run the patcher
        var runResult = await RunPatcher(
            Path.Combine(projectFolder, "bin", "Debug", "net9.0", "TestPatcher.dll"),
            new RunSynthesisMutagenPatcher
            {
                DataFolderPath = DataFolder,
                LoadOrderFilePath = PluginsPath,
                GameRelease = GameRelease.SkyrimSE,
                OutputPath = outputFile,
                ModKey = outputModKey.FileName
            });

        // Assert
        runResult.ExitCode.ShouldBe(0, $"Patcher run failed:\n{runResult.Output}");
        File.Exists(outputFile).ShouldBeTrue("Output mod file should exist");

        // Verify we can load the output mod
        using var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputFile, SkyrimRelease.SkyrimSE);
        outputMod.ModKey.ShouldBe(outputModKey);

        // Verify the patcher added the test NPC
        var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
        addedNpc.ShouldNotBeNull("Test NPC should exist in output");
        addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
    }

}
