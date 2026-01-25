using System.Reactive.Linq;
using Mutagen.Bethesda;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for file access denied error detection tests
/// Tests that we can detect and suggest fixes for file access denied errors
/// </summary>
public abstract class AccessDeniedErrorTest : IntegrationTest
{
    protected AccessDeniedErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [RetryFact(3)]
    public async Task AccessDeniedError_IsDetectedAndReported()
    {
        // Arrange

        // Create a test patcher that throws an access denied error
        var patcher = CreateSolutionPatcherWithSettings(
            "AccessDeniedPatcher",
            GenerateAccessDeniedErrorPatchContent(),
            nickname: "Access Denied Error Patcher");

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
    /// Generates patcher content that throws an access denied error
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateAccessDeniedErrorPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate a file access denied error");
            sb.AppendLine("var testPath = @\"C:\\Users\\Test\\Documents\\SomeFile.esp\";");
            sb.AppendLine("throw new System.IO.IOException($\"The process cannot access the file '{testPath}' because it is being used by another process.\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based access denied error detection test
/// </summary>
public class AccessDeniedErrorUIPipelineTest : AccessDeniedErrorTest
{
    public AccessDeniedErrorUIPipelineTest(ITestOutputHelper output) : base(output)
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
        patcherRun.ErrorClassification.ShouldBeOfType<AccessDeniedErrorVm>(
            "ErrorClassification should be AccessDeniedErrorVm");

        var vm = (AccessDeniedErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        Output.WriteLine($"File Path: {vm.FilePath}");

        vm.FilePath.ShouldNotBeNullOrEmpty("FilePath should be extracted from error message");
        Output.WriteLine("Successfully verified access denied error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based access denied error detection test
/// </summary>
public class AccessDeniedErrorCliPipelineTest : AccessDeniedErrorTest
{
    private Exception? _caughtException;

    public AccessDeniedErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains("File Access Denied"),
            "Should have logged the error classification");
        errorMessages.ShouldContain(msg => msg.Contains("cannot access a file") || msg.Contains("being used by another process"),
            "Should have logged the error message");

        Output.WriteLine("Successfully verified access denied error was detected and classified");
        return Task.CompletedTask;
    }
}
