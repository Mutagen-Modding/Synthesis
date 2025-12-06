using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Integration test for single Git patcher pipeline execution
/// </summary>
public class SingleGitPatcherPipelineTest : IntegrationTest
{
    public SingleGitPatcherPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task SingleGitPatcher_ProducesValidOutput()
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

        // Create a Git patcher repository
        var bareRepoPath = CreateGitPatcherRepository("TestPatcherRepo", AddTypicalPatcherNpc());

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[]
            {
                new GithubPatcherSettings
                {
                    On = true,
                    Nickname = "Test Patcher",
                    RemoteRepoPath = bareRepoPath,
                    SelectedProjectSubpath = "TestPatcher.csproj",
                    PatcherVersioning = PatcherVersioningEnum.Branch,
                    TargetBranch = "master",
                    FollowDefaultBranch = false,
                    MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                    SynthesisVersionType = PatcherNugetVersioningEnum.Profile
                }
            });

        // Act - Initialize and run
        await RunPatcherPipeline();

        // Assert - Check results
        EnsureActiveRunHasNoErrors();

        // Verify output file exists
        var outputPath = Path.Combine(DataFolder, $"{groupName}.esp");
        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist");

        // Verify we can load the output mod
        using var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE);
        var outputModKey = ModKey.FromNameAndExtension($"{groupName}.esp");
        outputMod.ModKey.ShouldBe(outputModKey);

        // Verify the patcher added the test NPC
        var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
        addedNpc.ShouldNotBeNull("Test NPC should exist in output");
        addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
    }
}
