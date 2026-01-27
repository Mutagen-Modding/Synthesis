using System.Reactive.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Noggog.StructuredStrings;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for TooManyMasters error detection tests
/// Tests that we can detect and suggest fixes for TooManyMasters errors when auto-split is disabled
/// </summary>
public abstract class TooManyMastersErrorTest : IntegrationTest
{
    protected TooManyMastersErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task TooManyMasters_IsDetectedAndReported()
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

        // Create a patcher that creates FormLists referencing all 256 masters
        var patcher = CreateSolutionPatcherWithSettings(
            "TooManyMastersPatcher",
            GenerateFormListsFor256Masters(),
            nickname: "TooManyMasters Patcher");

        var groupName = "Test Group";

        // Export settings with SplitIfMaxMastersExceeded set to FALSE
        // This will cause the TooManyMasters error instead of auto-splitting
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher },
            splitIfMaxMastersExceeded: false);

        // Act - Initialize and run
        await Act();

        // Assert - Check that the TooManyMasters error occurred
        await AssertErrorOccurred();

        // TODO: Future enhancement - check that the error was detected and specific suggestions were shown
        // (e.g., suggest enabling SplitIfMaxMastersExceeded)
    }

    /// <summary>
    /// Generates patcher code that creates FormLists referencing all 256 masters
    /// This will cause the patch to exceed the master limit
    /// </summary>
    private static Action<GameRelease, StructuredStringBuilder> GenerateFormListsFor256Masters()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Create FormLists that reference NPCs from all 256 master mods");
            sb.AppendLine("for (int i = 0; i < 256; i++)");
            sb.AppendLine("{");
            sb.AppendLine("    var formList = state.PatchMod.FormLists.AddNew();");
            sb.AppendLine("    formList.EditorID = $\"FormList{i:D3}\";");
            sb.AppendLine("    System.Console.WriteLine($\"Added {formList}\");");
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
            sb.AppendLine("            System.Console.WriteLine($\"Added {npc} reference\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("System.Console.WriteLine($\"{state.PatchMod.FormLists.Count} formlists added\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based TooManyMasters error detection test
/// </summary>
public class TooManyMastersErrorUIPipelineTest : TooManyMastersErrorTest
{
    public TooManyMastersErrorUIPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
    {
        await RunPatcherPipeline();
    }

    protected override async Task AssertErrorOccurred()
    {
        // In UI mode, the TooManyMasters exception is caught by SynthesisPipeline
        // and printed to Console.WriteLine (not ILogger), so it doesn't appear in OutputDisplay
        // The patcher completes "successfully" but with the error message printed to console
        var payload = GetStoredPayload();

        payload.ActiveRunVm.CurrentRun.ShouldNotBeNull("CurrentRun should be set");

        // Get the first (and only) patcher run
        var patcherRun = payload.ActiveRunVm.CurrentRun.Groups
            .SelectMany(g => g.Patchers)
            .FirstOrDefault();

        patcherRun.ShouldNotBeNull("Should have at least one patcher run");

        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");

        // The patcher errors out because TooManyMastersException is thrown when
        // SplitIfMaxMastersExceeded is false
        patcherRun.State.Value.ShouldBe(Synthesis.Bethesda.GUI.ViewModels.Profiles.Running.RunState.Error,
            "Patcher should error when TooManyMasters occurs with SplitIfMaxMastersExceeded = false");

        Output.WriteLine("Successfully verified patcher completed");

        // TODO: Future enhancement - capture Console.WriteLine output from the patcher process
        // The TooManyMasters error message is written to Console, not ILogger, so it's not in OutputDisplay
        // We would need to capture stdout from the patcher process to verify the error message
        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based TooManyMasters error detection test
/// </summary>
public class TooManyMastersErrorCliPipelineTest : TooManyMastersErrorTest
{
    private Exception? _caughtException;

    public TooManyMastersErrorCliPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        // Use RunPatcherPipeline component directly
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();

        // We expect this to throw or complete with an error
        try
        {
            await runPipeline.Run(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Expected - the patcher will throw an error
            _caughtException = ex;
            Output.WriteLine($"Run completed with expected error: {ex.Message}");
        }
    }

    protected override Task AssertErrorOccurred()
    {
        // Verify we caught an exception
        _caughtException.ShouldNotBeNull("Expected an exception to be thrown during patcher execution");

        Output.WriteLine($"Exception type: {_caughtException.GetType().FullName}");
        Output.WriteLine($"Exception message: {_caughtException.Message}");

        // The TooManyMastersException can be thrown directly, wrapped, or classified
        // Check if it's a ClassifiedErrorException (meaning it was properly classified)
        // OR if the exception string contains TooManyMastersException
        var isClassified = _caughtException is ClassifiedErrorException;
        var exceptionString = _caughtException.ToString();
        var containsTooManyMasters = exceptionString.Contains("TooManyMastersException", StringComparison.OrdinalIgnoreCase);

        (isClassified || containsTooManyMasters).ShouldBeTrue(
            $"Exception should either be classified or contain TooManyMastersException. " +
            $"Is classified: {isClassified}, Contains TooManyMasters: {containsTooManyMasters}");

        // In CLI mode, the actual error details are logged via ILogger
        // We capture these logs to verify the TooManyMasters error was properly surfaced
        var fullLog = LogSink.GetFullLog();
        Output.WriteLine("=== Captured Log Output ===");
        Output.WriteLine(fullLog);
        Output.WriteLine("=== End Log Output ===");

        var errorMessages = LogSink.ErrorMessages;
        Output.WriteLine($"=== Error Messages ({errorMessages.Count}) ===");
        foreach (var msg in errorMessages)
        {
            Output.WriteLine(msg);
        }
        Output.WriteLine("=== End Error Messages ===");

        // Verify that the error classification was detected and logged
        // Check error messages specifically (Serilog renders property values with quotes)
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(TooManyMastersError.ErrorTypeString),
            "Should have logged the error classification");

        Output.WriteLine("Successfully verified TooManyMasters error was detected and classified");
        return Task.CompletedTask;
    }
}
