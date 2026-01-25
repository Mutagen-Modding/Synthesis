using System.Reactive.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for missing mod reference error detection tests
/// Tests that we can detect and suggest fixes for errors where a referenced mod
/// was not present on the load order being sorted against
/// </summary>
public abstract class ReferencedModMissingErrorTest : IntegrationTest
{
    protected ReferencedModMissingErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [RetryFact(3)]
    public async Task MissingModReference_IsDetectedAndReported()
    {
        // Arrange

        // Create a test patcher that throws a NotImplementedException
        // for a referenced mod that's not in the load order
        var patcher = CreateSolutionPatcherWithSettings(
            "MissingModReferencePatcher",
            GenerateMissingModReferenceErrorPatchContent(),
            nickname: "Missing Mod Reference Patcher");

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

        // Act - Initialize and run
        await Act();

        // Assert - Check that the error occurred
        await AssertErrorOccurred();

        // TODO: Future enhancement - check that the error was detected and specific suggestions were shown
        // For now, this test will fail as expected since we haven't implemented error detection yet
    }

    /// <summary>
    /// Generates patcher content that throws a NotImplementedException
    /// for a missing mod reference
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateMissingModReferenceErrorPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate error when a referenced mod is not present in the load order");
            sb.AppendLine("// This can happen during load order sorting or mod dependency resolution");
            sb.AppendLine("throw new NotImplementedException(\"Referenced mod was not present on the load order being sorted against\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based missing mod reference error detection test
/// </summary>
public class ReferencedModMissingErrorUiPipelineTest : ReferencedModMissingErrorTest
{
    public ReferencedModMissingErrorUiPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
    {
        await RunPatcherPipeline();
    }

    protected override async Task AssertErrorOccurred()
    {
        // In UI mode, errors are captured in the PatcherRunVm's OutputDisplay TextDocument
        // This is what gets shown to the user in the GUI
        var payload = GetStoredPayload();

        payload.ActiveRunVm.CurrentRun.ShouldNotBeNull("CurrentRun should be set");

        // Get the first (and only) patcher run
        var patcherRun = payload.ActiveRunVm.CurrentRun.Groups
            .SelectMany(g => g.Patchers)
            .FirstOrDefault();

        patcherRun.ShouldNotBeNull("Should have at least one patcher run");

        // Verify the patcher is in error state
        patcherRun.State.Value.ShouldBe(Synthesis.Bethesda.GUI.ViewModels.Profiles.Running.RunState.Error,
            "Patcher should be in Error state");

        // Verify that the ErrorClassification is populated with the correct type
        patcherRun.ErrorClassification.ShouldNotBeNull("ErrorClassification should be populated");
        patcherRun.ErrorClassification.ShouldBeOfType<Synthesis.Bethesda.GUI.ViewModels.Profiles.Running.ReferencedModMissingErrorVm>(
            "ErrorClassification should be ReferencedModMissingErrorVm");

        var classification = (Synthesis.Bethesda.GUI.ViewModels.Profiles.Running.ReferencedModMissingErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {classification.ErrorType}");
        Output.WriteLine($"Error Message: {classification.Message}");
        Output.WriteLine("Successfully verified ReferencedModMissing error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based missing mod reference error detection test
/// </summary>
public class ReferencedModMissingErrorCliPipelineTest : ReferencedModMissingErrorTest
{
    private Exception? _caughtException;

    public ReferencedModMissingErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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

        // Verify exception type and exit code
        _caughtException.ShouldBeOfType<ClassifiedErrorException>();

        // In CLI mode, the actual error details are logged via ILogger
        // We capture these logs to verify the ReferencedModMissing error was properly surfaced
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains("Referenced Mod Missing"),
            "Should have logged the error classification");
        errorMessages.ShouldContain(msg => msg.Contains("Referenced mod was not present on the load order being sorted against"),
            "Should have logged the error suggestion");
        errorMessages.ShouldContain(msg => msg.Contains("Read more:") && msg.Contains("https://github.com/Mutagen-Modding/Synthesis/discussions/382"),
            "Should have logged the discussion link");

        Output.WriteLine("Successfully verified ReferencedModMissing error was detected and classified");
        return Task.CompletedTask;
    }
}
