using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Abstract base for single Git patcher pipeline execution tests
/// </summary>
public abstract class SingleGitPatcherPipelineTest : IntegrationTest
{
    protected SingleGitPatcherPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [RetryTheory(3)]
    [InlineData(PatcherVersioningEnum.Branch)]
    [InlineData(PatcherVersioningEnum.Commit)]
    [InlineData(PatcherVersioningEnum.Tag)]
    public async Task SingleGitPatcher_ProducesValidOutput(PatcherVersioningEnum versioningType)
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

        // Create a Git patcher repository and get commit info
        var bareRepoPath = CreateGitPatcherRepository("TestPatcherRepo", AddTypicalPatcherNpc());
        var commitSha = GetLatestCommitSha(bareRepoPath);
        var tagName = "v1.0.0";
        CreateTag(bareRepoPath, tagName);

        // Configure settings based on versioning type
        var settings = new GithubPatcherSettings
        {
            On = true,
            Nickname = "Test Patcher",
            RemoteRepoPath = bareRepoPath,
            SelectedProjectSubpath = "TestPatcher.csproj",
            PatcherVersioning = versioningType,
            MutagenVersionType = PatcherNugetVersioningEnum.Profile,
            SynthesisVersionType = PatcherNugetVersioningEnum.Profile
        };

        switch (versioningType)
        {
            case PatcherVersioningEnum.Branch:
                settings.TargetBranch = "master";
                settings.FollowDefaultBranch = false;
                settings.AutoUpdateToBranchTip = true;
                break;
            case PatcherVersioningEnum.Commit:
                settings.TargetCommit = commitSha;
                break;
            case PatcherVersioningEnum.Tag:
                settings.TargetTag = tagName;
                settings.LatestTag = true;  // Use true to automatically use the latest tag
                break;
        }

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { settings });

        // Act - Initialize and run
        await Act();

        // Assert - Check results
        await AssertNoErrors();

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

    protected abstract Task Act();

    protected virtual Task AssertNoErrors()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based single Git patcher pipeline test
/// </summary>
public class SingleGitPatcherUIPipelineTest : SingleGitPatcherPipelineTest
{
    public SingleGitPatcherUIPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
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
/// CLI-based single Git patcher pipeline test
/// </summary>
public class SingleGitPatcherCliPipelineTest : SingleGitPatcherPipelineTest
{
    public SingleGitPatcherCliPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        // Use RunPatcherPipeline component directly instead of RunPipelineLogic.Run
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }
}
