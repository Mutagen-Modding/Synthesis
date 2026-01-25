using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Abstract base for single solution patcher pipeline execution tests
/// </summary>
public abstract class SingleSolutionPatcherPipelineTest : IntegrationTest
{
    protected SingleSolutionPatcherPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [RetryFact(3)]
    public async Task SingleSolutionPatcher_ProducesValidOutput()
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

        // Create a test patcher project with settings
        var patcher = CreateSolutionPatcherWithSettings("TestPatcher", AddTypicalPatcherNpc(), nickname: "Test Patcher");

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

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
/// UI-based single solution patcher pipeline test
/// </summary>
public class SingleSolutionPatcherUIPipelineTest : SingleSolutionPatcherPipelineTest
{
    public SingleSolutionPatcherUIPipelineTest(ITestOutputHelper output) : base(output)
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
/// CLI-based single solution patcher pipeline test
/// </summary>
public class SingleSolutionPatcherCliPipelineTest : SingleSolutionPatcherPipelineTest
{
    public SingleSolutionPatcherCliPipelineTest(ITestOutputHelper output) : base(output)
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
