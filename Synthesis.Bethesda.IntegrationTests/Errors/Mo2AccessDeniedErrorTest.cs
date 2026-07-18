using System.Reactive.Linq;
using Autofac;
using Mutagen.Bethesda;
using ReactiveUI;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.IntegrationTests.Components;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for Mo2 access denied error detection tests.
/// Tests that when running inside MO2 and an access denied error occurs,
/// the error is classified as RanBuildInMo2ErrorClassification instead of AccessDeniedErrorClassification.
/// </summary>
public abstract class Mo2AccessDeniedErrorTest : IntegrationTest
{
    protected Mo2AccessDeniedErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task AccessDeniedError_InMo2_IsClassifiedAsRanBuildInMo2()
    {
        // Arrange
        // Create a test patcher that throws an access denied error
        var patcher = CreateSolutionPatcherWithSettings(
            "Mo2AccessDeniedPatcher",
            GenerateAccessDeniedErrorPatchContent(),
            nickname: "Mo2 Access Denied Error Patcher");

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

        // Act - Initialize and run (with Mo2 detection mocked to return true)
        await Act();

        // Assert - Check that the error was classified as RanBuildInMo2
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

    /// <summary>
    /// Configures the container to mock Mo2 environment detection as running inside Mo2
    /// </summary>
    protected void ConfigureMo2Detection(ContainerBuilder builder)
    {
        builder.RegisterInstance(new Mo2EnvironmentDetectorInjection(isRunningInsideMo2: true))
            .As<IMo2EnvironmentDetector>()
            .SingleInstance();
    }
}

/// <summary>
/// UI-based Mo2 access denied error detection test
/// </summary>
public class Mo2AccessDeniedErrorUIPipelineTest : Mo2AccessDeniedErrorTest
{
    public Mo2AccessDeniedErrorUIPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
    {
        await GetComponentPayloadAndInitialize<EmptyPayload>(ConfigureMo2Detection);
        var payload = GetStoredPayload();

        // Execute RunPatchers command on the UI thread
        Output.WriteLine("Executing RunPatchers command...");
        await Observable.Start(() =>
        {
            payload.ProfileManager.RunPatchers.Execute().Subscribe();
        }, payload.SchedulerProvider.MainThread);

        // Get the active run
        payload.ActiveRunVm.CurrentRun.ShouldNotBeNull("CurrentRun should be set after executing RunPatchers");

        // Wait for run to complete
        Output.WriteLine("Waiting for run to complete...");
        await payload.ActiveRunVm.CurrentRun.WhenAnyValue(x => x.Running)
            .Where(running => !running)
            .FirstAsync()
            .Timeout(TestTimeouts.Long);

        Output.WriteLine("Run completed");
    }

    protected override async Task AssertErrorOccurred()
    {
        // In UI mode, errors are captured in the PatcherRunVm's ErrorClassification
        var payload = GetStoredPayload();

        payload.ActiveRunVm.CurrentRun.ShouldNotBeNull("CurrentRun should be set");

        // Get the first (and only) patcher run
        var patcherRun = payload.ActiveRunVm.CurrentRun.Groups
            .SelectMany(g => g.Patchers)
            .FirstOrDefault();

        patcherRun.ShouldNotBeNull("Should have at least one patcher run");

        // Verify the patcher is in error state
        patcherRun.State.Value.ShouldBe(RunState.Error,
            "Patcher should be in Error state");

        // Verify that the ErrorClassification is RanBuildInMo2ErrorVm (not AccessDeniedErrorVm)
        patcherRun.ErrorClassification.ShouldNotBeNull("ErrorClassification should be populated");
        patcherRun.ErrorClassification.ShouldBeOfType<RanBuildInMo2ErrorVm>(
            "ErrorClassification should be RanBuildInMo2ErrorVm when running inside Mo2");

        var vm = (RanBuildInMo2ErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");

        vm.ErrorType.ShouldBe(RanBuildInMo2ErrorClassification.ErrorTypeString);
        Output.WriteLine("Successfully verified access denied error in Mo2 was classified as RanBuildInMo2");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based Mo2 access denied error detection test
/// </summary>
public class Mo2AccessDeniedErrorCliPipelineTest : Mo2AccessDeniedErrorTest
{
    private Exception? _caughtException;

    public Mo2AccessDeniedErrorCliPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        // Use RunPatcherPipeline component directly with Mo2 detection mocked
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>(ConfigureMo2Detection);

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

        // Verify that the error classification was detected and logged as RanBuildInMo2
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(RanBuildInMo2ErrorClassification.ErrorTypeString),
            "Should have logged the Mo2 error classification");
        errorMessages.ShouldContain(msg => msg.Contains("MO2's virtual file system"),
            "Should have logged the Mo2 error message");

        Output.WriteLine("Successfully verified access denied error in Mo2 was classified as RanBuildInMo2");
        return Task.CompletedTask;
    }
}
