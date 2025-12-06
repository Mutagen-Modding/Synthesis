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
/// Integration test for build meta caching - verifies that Git patchers are not rebuilt
/// when the build meta indicates they're already built
/// </summary>
public class BuildMetaCachingTest : IntegrationTest
{
    public BuildMetaCachingTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task BuildMetaCaching_SkipsRebuildOnSecondRun()
    {
        // Arrange - Create test mod and Git patcher
        var testModKey = ModKey.FromNameAndExtension("TestMod.esp");
        CreateSimpleSkyrimMod(testModKey, mod =>
        {
            var npc = mod.Npcs.AddNew();
            npc.Name = "Integration Test NPC";
        });
        AddToLoadOrder(testModKey, enabled: true);

        // Create a Git patcher repository
        var bareRepoPath = CreateGitPatcherRepository("TestPatcherRepo",AddTypicalPatcherNpc());

        var groupName = "Test Group";
        var patchers = new[]
        {
            new GithubPatcherSettings
            {
                ID = "SomeID",
                On = true,
                Nickname = "Test Patcher",
                RemoteRepoPath = bareRepoPath,
                SelectedProjectSubpath = "TestPatcher.csproj",
                PatcherVersioning = PatcherVersioningEnum.Branch,
                TargetBranch = "master",
                MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                SynthesisVersionType = PatcherNugetVersioningEnum.Profile
            }
        };

        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: patchers);

        // Act - First run: Build and run the patcher (should compile)
        await RunPatcherPipeline();

        // Assert - First run completed successfully
        EnsureActiveRunHasNoErrors();

        var outputPath = Path.Combine(DataFolder, $"{groupName}.esp");
        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after first run");

        // Verify the patcher ran correctly
        using (var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE))
        {
            var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
            addedNpc.ShouldNotBeNull("Test NPC should exist in output from first run");
            addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
        }

        // Delete the output file to ensure the second run actually produces it
        File.Delete(outputPath);

        // Act - Second run: Run with ThrowingBuild to verify build is skipped
        // The build meta should cause the system to skip compilation
        await RunPatcherPipelineWithThrowingBuild();

        // Assert - Second run completed successfully without calling IBuild.Compile
        EnsureActiveRunHasNoErrors();

        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after second run");

        // Verify the patcher ran correctly on second run
        using (var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE))
        {
            var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
            addedNpc.ShouldNotBeNull("Test NPC should exist in output from second run");
            addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
        }
    }
}
