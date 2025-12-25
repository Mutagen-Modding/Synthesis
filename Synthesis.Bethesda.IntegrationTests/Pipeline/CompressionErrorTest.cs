using System.Reactive.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

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

        // Assert - Check that the error occurred
        await AssertErrorOccurred();

        // TODO: Future enhancement - check that the error was detected and specific suggestions were shown
        // For now, this test will fail as expected since we haven't implemented error detection yet
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

        // In UI mode, the error details flow through IRunReporter which logs to ILogger
        // Similar to CLI mode, we can capture those logs
        // For now, verify that the patcher entered error state
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine("Successfully verified patcher entered error state");

        // TODO: Future enhancement - capture the error details from logged output
        // The error is logged via IRunReporter -> ILogger, similar to CLI mode
        // We could set up LogSink for UI mode as well to capture this
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

        // Verify exception type and exit code
        _caughtException.ShouldBeOfType<Synthesis.Bethesda.Execution.CliUnsuccessfulRunException>();
        var cliException = (Synthesis.Bethesda.Execution.CliUnsuccessfulRunException)_caughtException;
        cliException.ExitCode.ShouldNotBe(0, "Exit code should be non-zero indicating failure");

        return Task.CompletedTask;
    }
}
