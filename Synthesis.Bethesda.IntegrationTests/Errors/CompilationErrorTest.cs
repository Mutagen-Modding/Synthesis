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
/// Abstract base for compilation error detection tests.
/// Tests that we can detect and classify SynthesisBuildFailure errors that occur
/// when a patcher fails to compile, typically due to API changes in newer libraries.
/// </summary>
public abstract class CompilationErrorTest : IntegrationTest
{
    protected CompilationErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task CompilationError_IsDetectedAndReported()
    {
        // Arrange
        // Create a test patcher with a syntax error so it fails to compile
        var patcher = CreateSolutionPatcherWithSettings(
            "CompilationErrorPatcher",
            GenerateCompilationErrorPatchContent(),
            nickname: "Compilation Error Patcher");

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
    /// Generates patcher content that contains invalid C# code so the patcher fails to compile.
    /// This triggers a SynthesisBuildFailure during the build phase.
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateCompilationErrorPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// This code has a syntax error that will cause a compilation failure");
            sb.AppendLine("this is not valid C# code <<<>>>;");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based compilation error detection test
/// </summary>
public class CompilationErrorUIPipelineTest : CompilationErrorTest
{
    public CompilationErrorUIPipelineTest(ITestOutputHelper output) : base(output)
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
        patcherRun.ErrorClassification.ShouldBeOfType<CompilationErrorVm>(
            "ErrorClassification should be CompilationErrorVm");

        var vm = (CompilationErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        Output.WriteLine($"Discussion Link: {vm.DiscussionLink}");
        Output.WriteLine("Successfully verified compilation error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based compilation error detection test
/// </summary>
public class CompilationErrorCliPipelineTest : CompilationErrorTest
{
    private Exception? _caughtException;

    public CompilationErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(CompilationErrorClassification.ErrorTypeString),
            "Should have logged the error classification");
        errorMessages.ShouldContain(msg => msg.Contains("failed to compile") || msg.Contains("incompatible"),
            "Should have logged the error suggestion");
        errorMessages.ShouldContain(msg => msg.Contains("Read more:") && msg.Contains("Versioning"),
            "Should have logged the discussion link");

        Output.WriteLine("Successfully verified compilation error was detected and classified");
        return Task.CompletedTask;
    }
}
