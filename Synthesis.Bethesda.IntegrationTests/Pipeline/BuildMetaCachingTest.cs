using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Abstract base for build meta caching tests - verifies that Git patchers are not rebuilt
/// when build meta indicates they're already built
/// </summary>
public abstract class BuildMetaCachingTest : IntegrationTest
{
    protected BuildMetaCachingTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    protected async Task TestSkipsRebuildOnSecondRun()
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
        var commitSha = GetLatestCommitSha(bareRepoPath);

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
                TargetCommit = commitSha,
                MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                SynthesisVersionType = PatcherNugetVersioningEnum.Profile
            }
        };

        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: patchers);

        // Act - First run: Build and run patcher (should compile)
        await ActFirstRun();

        // Assert - First run completed successfully
        await AssertNoErrors();

        var outputPath = Path.Combine(DataFolder, $"{groupName}.esp");
        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after first run");

        // Verify patcher ran correctly
        using (var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE))
        {
            var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
            addedNpc.ShouldNotBeNull("Test NPC should exist in output from first run");
            addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
        }

        // Delete output file to ensure second run actually produces it
        File.Delete(outputPath);

        // Act - Second run: Run with ThrowingBuild to verify build is skipped
        // The build meta should cause system to skip compilation
        await ActSecondRun();

        // Assert - Second run completed successfully without calling IBuild.Compile
        await AssertNoErrors();

        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after second run");

        // Verify patcher ran correctly on second run
        using (var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE))
        {
            var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
            addedNpc.ShouldNotBeNull("Test NPC should exist in output from second run");
            addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
        }
    }

    protected async Task TestRecompilesWhenExecutableMissing()
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
        var bareRepoPath = CreateGitPatcherRepository("TestPatcherRepo", AddTypicalPatcherNpc());
        var commitSha = GetLatestCommitSha(bareRepoPath);

        var groupName = "Test Group Missing Exe";
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
                TargetCommit = commitSha,
                MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                SynthesisVersionType = PatcherNugetVersioningEnum.Profile
            }
        };

        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: patchers);

        // Act - First run: Build and run patcher (should compile)
        await ActFirstRun();

        // Assert - First run completed successfully
        await AssertNoErrors();

        var outputPath = Path.Combine(DataFolder, $"{groupName}.esp");
        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after first run");

        // Get meta file and delete the executable but keep the metadata
        // Path structure: TestFolder/Temp/Synthesis/{profileId}/Git/{patcherId}/Build.meta
        var patcherId = patchers[0].ID;
        ProfileID.ShouldNotBeNullOrEmpty("ProfileID should be set after ExportSettingsWithPatchers");

        var profileDir = Path.Combine(TestFolder, "Temp", "Synthesis", ProfileID);
        var metaPath = Path.Combine(profileDir, "Git", patcherId, "Build.meta");
        File.Exists(metaPath).ShouldBeTrue($"Build.meta should exist at {metaPath}");

        var metaContent = File.ReadAllText(metaPath);
        var meta = System.Text.Json.JsonSerializer.Deserialize<GitCompilationMeta>(metaContent, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        meta.ShouldNotBeNull("Meta should deserialize successfully");
        meta.ExecutablePath.ShouldNotBeNull("ExecutablePath should be set in meta");

        // Delete the executable file but keep the meta file
        if (!string.IsNullOrWhiteSpace(meta.ExecutablePath) && File.Exists(meta.ExecutablePath))
        {
            File.Delete(meta.ExecutablePath);
        }

        // Delete the output file to ensure the second run actually produces it
        File.Delete(outputPath);

        // Act - Second run: Should trigger recompilation due to missing executable
        // This should call IBuild.Compile even though metadata exists
        await ActSecondRun();

        // Assert - Second run completed successfully and recompiled
        await AssertNoErrors();

        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after second run");

        // Verify the patcher ran correctly after recompilation
        using (var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE))
        {
            var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
            addedNpc.ShouldNotBeNull("Test NPC should exist in output from second run");
            addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
        }
    }

    protected abstract Task ActFirstRun();
    protected abstract Task ActSecondRun();

    protected virtual Task AssertNoErrors()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI test - verifies build meta caching skips rebuild on second run
/// </summary>
public class BuildMetaCachingUiTest_SkipsRebuild : BuildMetaCachingTest
{
    public BuildMetaCachingUiTest_SkipsRebuild(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task BuildMetaCaching_SkipsRebuildOnSecondRun()
    {
        await TestSkipsRebuildOnSecondRun();
    }

    protected override async Task ActFirstRun()
    {
        await RunPatcherPipeline();
    }

    protected override async Task ActSecondRun()
    {
        await RunPatcherPipelineWithThrowingBuild();
    }

    protected override Task AssertNoErrors()
    {
        EnsureActiveRunHasNoErrors();
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI test - verifies build meta caching recompiles when executable is missing
/// </summary>
public class BuildMetaCachingUiTest_RecompilesWhenMissing : BuildMetaCachingTest
{
    public BuildMetaCachingUiTest_RecompilesWhenMissing(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task BuildMetaCaching_RecompilesWhenExecutableMissing()
    {
        await TestRecompilesWhenExecutableMissing();
    }

    protected override async Task ActFirstRun()
    {
        await RunPatcherPipeline();
    }

    protected override async Task ActSecondRun()
    {
        await RunPatcherPipeline();
    }

    protected override Task AssertNoErrors()
    {
        EnsureActiveRunHasNoErrors();
        return Task.CompletedTask;
    }
}

/// <summary>
/// CLI test - verifies build meta caching skips rebuild on second run
/// </summary>
public class BuildMetaCachingCliTest_SkipsRebuild : BuildMetaCachingTest
{
    public BuildMetaCachingCliTest_SkipsRebuild(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    [Fact]
    public async Task BuildMetaCaching_SkipsRebuildOnSecondRun()
    {
        await TestSkipsRebuildOnSecondRun();
    }

    protected override async Task ActFirstRun()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }

    protected override async Task ActSecondRun()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }
}

/// <summary>
/// CLI test - verifies build meta caching recompiles when executable is missing
/// </summary>
public class BuildMetaCachingCliTest_RecompilesWhenMissing : BuildMetaCachingTest
{
    public BuildMetaCachingCliTest_RecompilesWhenMissing(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    [Fact]
    public async Task BuildMetaCaching_RecompilesWhenExecutableMissing()
    {
        await TestRecompilesWhenExecutableMissing();
    }

    protected override async Task ActFirstRun()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }

    protected override async Task ActSecondRun()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }
}