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
/// Abstract base for csc.exe crash error detection tests.
/// Tests that we detect the specific "csc.exe exited with code -1073741819" pattern
/// and classify it separately from generic compilation errors.
/// </summary>
public abstract class CscCrashErrorTest : IntegrationTest
{
    protected CscCrashErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task CscCrashError_IsDetectedAndReported()
    {
        // Arrange
        var patcher = CreateSolutionPatcherWithSettings(
            "CscCrashPatcher",
            GenerateCscCrashErrorPatchContent(),
            nickname: "Csc Crash Error Patcher");

        ExportSettingsWithPatchers(
            groupName: "Test Group",
            patchers: new[] { patcher });

        // Act
        await Act();

        // Assert
        await AssertErrorOccurred();
    }

    /// <summary>
    /// Generates patcher content that throws an exception matching the csc.exe crash pattern
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateCscCrashErrorPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate csc.exe crash (access violation)");
            sb.AppendLine("throw new System.Exception(\"error MSB6006: \\\"csc.exe\\\" exited with code -1073741819.\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based csc crash error detection test
/// </summary>
public class CscCrashErrorUIPipelineTest : CscCrashErrorTest
{
    public CscCrashErrorUIPipelineTest(ITestOutputHelper output) : base(output)
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

        var patcherRun = payload.ActiveRunVm.CurrentRun.Groups
            .SelectMany(g => g.Patchers)
            .FirstOrDefault();

        patcherRun.ShouldNotBeNull("Should have at least one patcher run");

        patcherRun.State.Value.ShouldBe(Synthesis.Bethesda.GUI.ViewModels.Profiles.Running.RunState.Error,
            "Patcher should be in Error state");

        patcherRun.ErrorClassification.ShouldNotBeNull("ErrorClassification should be populated");
        patcherRun.ErrorClassification.ShouldBeOfType<CscCrashErrorVm>(
            "ErrorClassification should be CscCrashErrorVm");

        var vm = (CscCrashErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        vm.ErrorType.ShouldBe(CscCrashErrorClassification.ErrorTypeString);
        Output.WriteLine("Successfully verified csc crash error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based csc crash error detection test
/// </summary>
public class CscCrashErrorCliPipelineTest : CscCrashErrorTest
{
    private Exception? _caughtException;

    public CscCrashErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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
        _caughtException.ShouldNotBeNull("Expected an exception to be thrown during patcher execution");

        Output.WriteLine($"Exception type: {_caughtException.GetType().FullName}");
        Output.WriteLine($"Exception message: {_caughtException.Message}");

        _caughtException.ShouldBeOfType<ClassifiedErrorException>();

        var fullLog = LogSink.GetFullLog();
        Output.WriteLine("=== Captured Log Output ===");
        Output.WriteLine(fullLog);
        Output.WriteLine("=== End Log Output ===");

        var errorMessages = LogSink.ErrorMessages;
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(CscCrashErrorClassification.ErrorTypeString),
            "Should have logged the csc crash error classification");

        Output.WriteLine("Successfully verified csc crash error was detected and classified");
        return Task.CompletedTask;
    }
}
