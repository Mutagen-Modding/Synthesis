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
/// Abstract base for two solution patcher pipeline execution tests
/// </summary>
public abstract class TwoSolutionPatchersPipelineTest : IntegrationTest
{
    protected TwoSolutionPatchersPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [RetryFact(3)]
    public async Task TwoSolutionPatchers_BothProduceOutputInSameMod()
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

        // Create two test patcher projects with settings
        var firstPatcher = CreateSolutionPatcherWithSettings(
            "FirstPatcher",
            AddNpcToPatcher("FirstPatcherNPC", "First Patcher Was Here"),
            nickname: "First Patcher");

        var secondPatcher = CreateSolutionPatcherWithSettings(
            "SecondPatcher",
            AddNpcToPatcher("SecondPatcherNPC", "Second Patcher Was Here"),
            nickname: "Second Patcher");

        // Export settings with both patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { firstPatcher, secondPatcher });

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

        // Verify the first patcher added its NPC
        var firstNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "FirstPatcherNPC");
        firstNpc.ShouldNotBeNull("First patcher NPC should exist in output");
        firstNpc.Name?.String.ShouldBe("First Patcher Was Here");

        // Verify the second patcher added its NPC
        var secondNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "SecondPatcherNPC");
        secondNpc.ShouldNotBeNull("Second patcher NPC should exist in output");
        secondNpc.Name?.String.ShouldBe("Second Patcher Was Here");
    }

    protected abstract Task Act();

    protected virtual Task AssertNoErrors()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based two solution patcher pipeline test
/// </summary>
public class TwoSolutionPatchersUIPipelineTest : TwoSolutionPatchersPipelineTest
{
    public TwoSolutionPatchersUIPipelineTest(ITestOutputHelper output) : base(output)
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
/// CLI-based two solution patcher pipeline test
/// </summary>
public class TwoSolutionPatchersCliPipelineTest : TwoSolutionPatchersPipelineTest
{
    public TwoSolutionPatchersCliPipelineTest(ITestOutputHelper output) : base(output)
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
