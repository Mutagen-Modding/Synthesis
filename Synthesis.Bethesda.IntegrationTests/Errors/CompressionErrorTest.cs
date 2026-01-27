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
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for compression error detection tests
/// Tests that we can detect and suggest fixes for known compression errors
///
/// Example usage with Git patcher:
/// var patcher = CreateGitPatcherWithSettings(
///     "MyRepo",
///     GenerateCompressionErrorPatchContent(),
///     additionalPackageReferences: new[] { new PackageReference("Ionic.Zip", "1.9.1.8") });
/// </summary>
public abstract class CompressionErrorTest : IntegrationTest
{
    protected CompressionErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task CompressionError_IsDetectedAndReported()
    {
        // Arrange

        // Create a test patcher that throws a compression error
        // This uses the Ionic.Zip package to throw a realistic compression error
        var patcher = CreateSolutionPatcherWithSettings(
            "CompressionFailurePatcher",
            GenerateCompressionErrorPatchContent(),
            nickname: "Compression Error Patcher",
            additionalPackageReferences: new[] { new PackageReference("Ionic.Zip", "1.9.1.8") });

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
    /// Generates patcher content that throws a compression error
    /// This simulates the error from CompressionFailureForced patcher
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateCompressionErrorPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate a compression error that can occur when reading corrupted mod files");
            sb.AppendLine("// This is the error message from Ionic.Zlib when it encounters bad compression data");
            sb.AppendLine("throw new Ionic.Zlib.ZlibException(\"Bad state (unknown compression method (0x0B))\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based compression error detection test
/// </summary>
public class CompressionErrorUIPipelineTest : CompressionErrorTest
{
    public CompressionErrorUIPipelineTest(ITestOutputHelper output) : base(output)
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
        patcherRun.ErrorClassification.ShouldBeOfType<CompressionErrorVm>(
            "ErrorClassification should be CompressionErrorVm");

        var vm = (CompressionErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        Output.WriteLine("Successfully verified compression error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based compression error detection test
/// </summary>
public class CompressionErrorCliPipelineTest : CompressionErrorTest
{
    private Exception? _caughtException;

    public CompressionErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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
        // We capture these logs to verify the compression error was properly surfaced
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(CompressionErrorClassification.ErrorTypeString),
            "Should have logged the error classification");
        errorMessages.ShouldContain(msg => msg.Contains("compression error occurred") || msg.Contains("corrupted or was compressed"),
            "Should have logged the error suggestion");

        Output.WriteLine("Successfully verified compression error was detected and classified");
        return Task.CompletedTask;
    }
}
