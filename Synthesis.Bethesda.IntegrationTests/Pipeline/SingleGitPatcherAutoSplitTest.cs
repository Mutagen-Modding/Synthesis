using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog.StructuredStrings;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Abstract base for single Git patcher auto-split tests - verifies that a Git patcher
/// creating too many masters results in split output files
/// </summary>
public abstract class SingleGitPatcherAutoSplitTest : IntegrationTest
{
    protected SingleGitPatcherAutoSplitTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task SingleGitPatcher_CreatesTooManyMasters_CreatesSplitFiles()
    {
        // Arrange - Create 256 master mods to exceed the 254 master limit
        for (int i = 0; i < 256; i++)
        {
            var masterModKey = ModKey.FromNameAndExtension($"Master{i:D3}.esp");
            CreateSimpleSkyrimMod(masterModKey, mod =>
            {
                var npc = mod.Npcs.AddNew();
                npc.Name = $"Master {i} NPC";
                npc.EditorID = $"Master{i}NPC";
            });
            AddToLoadOrder(masterModKey, enabled: true);
        }

        // Create a Git patcher that creates FormLists referencing all 256 masters
        var patcher = CreateGitPatcherWithSettings(
            "AutoSplitPatcher",
            GenerateFormListsFor256Masters(),
            nickname: "AutoSplit Patcher");

        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

        // Act
        await Act();

        // Assert
        await AssertNoErrors();

        // Verify split files were created
        // First split has no suffix (base name), second split is _2
        var splitFile1Path = Path.Combine(DataFolder, $"{groupName}.esp");
        var splitFile2Path = Path.Combine(DataFolder, $"{groupName}_2.esp");

        File.Exists(splitFile1Path).ShouldBeTrue("First split file should exist");
        File.Exists(splitFile2Path).ShouldBeTrue("Second split file should exist");

        // Verify the split files can be loaded
        using var split1Mod = SkyrimMod.CreateFromBinaryOverlay(splitFile1Path, SkyrimRelease.SkyrimSE);
        using var split2Mod = SkyrimMod.CreateFromBinaryOverlay(splitFile2Path, SkyrimRelease.SkyrimSE);

        // Verify FormLists were created across split files
        var totalFormLists = split1Mod.FormLists.Count + split2Mod.FormLists.Count;
        totalFormLists.ShouldBe(256, "Should have created 256 FormLists across split files");
    }

    private static Action<GameRelease, StructuredStringBuilder> GenerateFormListsFor256Masters()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Create FormLists that reference NPCs from all 256 master mods");
            sb.AppendLine("for (int i = 0; i < 256; i++)");
            sb.AppendLine("{");
            sb.AppendLine("    var formList = state.PatchMod.FormLists.AddNew();");
            sb.AppendLine("    formList.EditorID = $\"FormList{i:D3}\";");
            sb.AppendLine();
            sb.AppendLine("    // Try to find the NPC from the corresponding master");
            sb.AppendLine("    var masterModKey = Mutagen.Bethesda.Plugins.ModKey.FromNameAndExtension($\"Master{i:D3}.esp\");");
            sb.AppendLine("    var masterMod = state.LoadOrder.TryGetValue(masterModKey);");
            sb.AppendLine("    if (masterMod != null)");
            sb.AppendLine("    {");
            sb.AppendLine("        var npc = masterMod.Mod?.Npcs.FirstOrDefault(n => n.EditorID == $\"Master{i}NPC\");");
            sb.AppendLine("        if (npc != null)");
            sb.AppendLine("        {");
            sb.AppendLine("            formList.Items.Add(npc.ToLink());");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertNoErrors()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based single Git patcher auto-split test
/// </summary>
public class SingleGitPatcherAutoSplitUiTest : SingleGitPatcherAutoSplitTest
{
    public SingleGitPatcherAutoSplitUiTest(ITestOutputHelper output) : base(output)
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
/// CLI-based single Git patcher auto-split test
/// </summary>
public class SingleGitPatcherAutoSplitCliTest : SingleGitPatcherAutoSplitTest
{
    public SingleGitPatcherAutoSplitCliTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }
}
