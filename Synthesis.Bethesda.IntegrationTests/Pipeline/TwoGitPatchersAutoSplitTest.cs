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
/// Abstract base for two Git patcher auto-split tests - verifies that when the first patcher
/// creates too many masters (resulting in split files), the second patcher can modify content
/// from the first patcher while maintaining the split file structure
/// </summary>
public abstract class TwoGitPatchersAutoSplitTest : IntegrationTest
{
    protected TwoGitPatchersAutoSplitTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task TwoGitPatchers_FirstCreatesSplitFiles_SecondModifiesContent()
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

        // Create first Git patcher that creates FormLists referencing all 256 masters
        var firstPatcher = CreateGitPatcherWithSettings(
            "FirstAutoSplitPatcher",
            GenerateFormListsFor256Masters(),
            nickname: "First AutoSplit Patcher");

        // Create second Git patcher that modifies all FormLists by renaming their EDIDs
        var secondPatcher = CreateGitPatcherWithSettings(
            "SecondModifyPatcher",
            GenerateFormListModifier(),
            nickname: "Second Modify Patcher");

        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { firstPatcher, secondPatcher });

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

        // Verify that the second patcher modified all FormLists by renaming their EDIDs
        // The EDIDs should now have "_Modified" suffix
        var allFormLists = split1Mod.FormLists
            .Concat(split2Mod.FormLists)
            .ToList();

        foreach (var formList in allFormLists)
        {
            formList.EditorID.ShouldNotBeNull("FormList should have an EditorID");
            formList.EditorID!.EndsWith("_Modified").ShouldBeTrue($"FormList EditorID '{formList.EditorID}' should have been modified by second patcher");
        }

        // Verify all 256 FormLists were modified
        var modifiedCount = allFormLists.Count(fl => fl.EditorID?.EndsWith("_Modified") == true);
        modifiedCount.ShouldBe(256, "All 256 FormLists should have been modified");
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

    private static Action<GameRelease, StructuredStringBuilder> GenerateFormListModifier()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Modify all FormLists by renaming their EditorIDs");
            sb.AppendLine("foreach (var formList in state.LoadOrder.PriorityOrder.FormList().WinningOverrides())");
            sb.AppendLine("{");
            sb.AppendLine("    if (formList.EditorID != null && formList.EditorID.StartsWith(\"FormList\"))");
            sb.AppendLine("    {");
            sb.AppendLine("        var modifiedFormList = state.PatchMod.FormLists.GetOrAddAsOverride(formList);");
            sb.AppendLine("        modifiedFormList.EditorID = formList.EditorID + \"_Modified\";");
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
/// UI-based two Git patcher auto-split test
/// </summary>
public class TwoGitPatchersAutoSplitUiTest : TwoGitPatchersAutoSplitTest
{
    public TwoGitPatchersAutoSplitUiTest(ITestOutputHelper output) : base(output)
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
/// CLI-based two Git patcher auto-split test
/// </summary>
public class TwoGitPatchersAutoSplitCliTest : TwoGitPatchersAutoSplitTest
{
    public TwoGitPatchersAutoSplitCliTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }
}
