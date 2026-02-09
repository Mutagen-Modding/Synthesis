using Mutagen.Bethesda;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for library version mismatch error detection tests.
/// Tests that we can detect and classify MissingMethodException errors that occur
/// when Mutagen and Synthesis library versions are incompatible.
/// </summary>
public abstract class LibraryVersionMismatchErrorTest : IntegrationTest
{
    protected LibraryVersionMismatchErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task LibraryVersionMismatchError_IsDetectedAndReported()
    {
        // Arrange
        // Create a test patcher that throws a MissingMethodException simulating a version mismatch
        var patcher = CreateSolutionPatcherWithSettings(
            "VersionMismatchPatcher",
            GenerateVersionMismatchErrorPatchContent(),
            nickname: "Version Mismatch Patcher");

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

        // Act - Initialize and run
        await Act();

        // Assert - Check that the error occurred and was properly classified
        await AssertErrorOccurred();
    }

    /// <summary>
    /// Generates patcher content that throws a MissingMethodException simulating a version mismatch.
    /// This replicates the error that occurs when an older version of Mutagen is paired with
    /// a newer version of Synthesis, or vice versa.
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateVersionMismatchErrorPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate a library version mismatch error");
            sb.AppendLine("// This is the error that occurs when library versions are incompatible");
            sb.AppendLine("throw new System.MissingMethodException(\"Method not found: 'Void SomeLibrary.SomeClass.SomeMethod()'.\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based library version mismatch error detection test
/// </summary>
public class LibraryVersionMismatchErrorUIPipelineTest : LibraryVersionMismatchErrorTest
{
    public LibraryVersionMismatchErrorUIPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
    {
        await RunPatcherPipeline();
    }

    protected override async Task AssertErrorOccurred()
    {
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
        patcherRun.ErrorClassification.ShouldBeOfType<LibraryVersionMismatchErrorVm>(
            "ErrorClassification should be LibraryVersionMismatchErrorVm");

        var vm = (LibraryVersionMismatchErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        Output.WriteLine($"Discussion Link: {vm.DiscussionLink}");
        Output.WriteLine("Successfully verified library version mismatch error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based library version mismatch error detection test
/// </summary>
public class LibraryVersionMismatchErrorCliPipelineTest : LibraryVersionMismatchErrorTest
{
    private Exception? _caughtException;

    public LibraryVersionMismatchErrorCliPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();

        try
        {
            await runPipeline.Run(CancellationToken.None);
        }
        catch (Exception ex)
        {
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

        // Verify exception type - should be ClassifiedErrorException when error is classified
        _caughtException.ShouldBeOfType<ClassifiedErrorException>();

        // In CLI mode, the actual error details are logged via ILogger
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(LibraryVersionMismatchErrorClassification.ErrorTypeString),
            "Should have logged the error classification");
        errorMessages.ShouldContain(msg => msg.Contains("version mismatch") || msg.Contains("incompatible APIs"),
            "Should have logged the error suggestion");
        errorMessages.ShouldContain(msg => msg.Contains("Read more:") && msg.Contains("Versioning"),
            "Should have logged the discussion link");

        Output.WriteLine("Successfully verified library version mismatch error was detected and classified");
        return Task.CompletedTask;
    }
}
