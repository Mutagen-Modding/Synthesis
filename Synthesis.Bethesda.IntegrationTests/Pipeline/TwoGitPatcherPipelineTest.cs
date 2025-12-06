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
/// Integration test for two Git patcher pipeline execution
/// </summary>
public class TwoGitPatcherPipelineTest : IntegrationTest
{
    public TwoGitPatcherPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task TwoGitPatchers_BothProduceOutputInSameMod()
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

        // Create two Git patcher repositories
        var bareRepoPath1 = CreateGitPatcherRepository("FirstPatcherRepo", AddNpcToPatcher("FirstPatcherNPC", "First Patcher Was Here"));
        var bareRepoPath2 = CreateGitPatcherRepository("SecondPatcherRepo", AddNpcToPatcher("SecondPatcherNPC", "Second Patcher Was Here"));

        // Export settings with both patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[]
            {
                new GithubPatcherSettings
                {
                    On = true,
                    Nickname = "First Patcher",
                    RemoteRepoPath = bareRepoPath1,
                    SelectedProjectSubpath = "TestPatcher.csproj",
                    PatcherVersioning = PatcherVersioningEnum.Branch,
                    TargetBranch = "master",
                    MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                    SynthesisVersionType = PatcherNugetVersioningEnum.Profile
                },
                new GithubPatcherSettings
                {
                    On = true,
                    Nickname = "Second Patcher",
                    RemoteRepoPath = bareRepoPath2,
                    SelectedProjectSubpath = "TestPatcher.csproj",
                    PatcherVersioning = PatcherVersioningEnum.Branch,
                    TargetBranch = "master",
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

        // Verify the first patcher added its NPC
        var firstNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "FirstPatcherNPC");
        firstNpc.ShouldNotBeNull("First patcher NPC should exist in output");
        firstNpc.Name?.String.ShouldBe("First Patcher Was Here");

        // Verify the second patcher added its NPC
        var secondNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "SecondPatcherNPC");
        secondNpc.ShouldNotBeNull("Second patcher NPC should exist in output");
        secondNpc.Name?.String.ShouldBe("Second Patcher Was Here");
    }
}
