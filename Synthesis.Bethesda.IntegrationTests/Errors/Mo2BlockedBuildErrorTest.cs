using System.Reactive.Linq;
using Autofac;
using Mutagen.Bethesda;
using ReactiveUI;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Exceptions;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.IntegrationTests.Components;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Abstract base for Mo2 blocked build error detection tests.
/// Tests that when running inside MO2 with BlockBuildingWithinMo2 setting enabled,
/// the build is blocked and error is classified as RanBuildInMo2ErrorClassification.
/// </summary>
public abstract class Mo2BlockedBuildErrorTest : IntegrationTest
{
    protected Mo2BlockedBuildErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task BlockBuildingWithinMo2_InMo2_BlocksBuildAndClassifiesError()
    {
        // Arrange
        // Create a simple patcher (the content doesn't matter - build will be blocked before execution)
        var patcher = CreateSolutionPatcherWithSettings(
            "Mo2BlockedPatcher",
            AddTypicalPatcherNpc(),
            nickname: "Mo2 Blocked Build Patcher");

        // Export settings with patchers
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

        // Act - Initialize and run (with Mo2 detection and BlockBuildingWithinMo2 both enabled)
        await Act();

        // Assert - Check that the error was classified as RanBuildInMo2
        await AssertErrorOccurred();
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures the container to mock Mo2 environment detection and enable BlockBuildingWithinMo2
    /// </summary>
    protected void ConfigureMo2AndBlockSetting(ContainerBuilder builder)
    {
        // Mock Mo2 detection as running inside Mo2
        builder.RegisterInstance(new Mo2EnvironmentDetectorInjection(isRunningInsideMo2: true))
            .As<IMo2EnvironmentDetector>()
            .SingleInstance();

        // Enable BlockBuildingWithinMo2 setting
        builder.RegisterInstance(new BlockBuildingWithinMo2Injection(blockBuildingWithinMo2: true))
            .As<IBlockBuildingWithinMo2SettingsProvider>()
            .SingleInstance();
    }
}

/// <summary>
/// UI-based Mo2 blocked build error detection test
/// </summary>
public class Mo2BlockedBuildErrorUIPipelineTest : Mo2BlockedBuildErrorTest
{
    public Mo2BlockedBuildErrorUIPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
    {
        await GetComponentPayloadAndInitialize<EmptyPayload>(ConfigureMo2AndBlockSetting);
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
            "Patcher should be in Error state due to blocked build");

        // Verify that the ErrorClassification is Mo2BuildBlockedErrorVm
        patcherRun.ErrorClassification.ShouldNotBeNull("ErrorClassification should be populated");
        patcherRun.ErrorClassification.ShouldBeOfType<Mo2BuildBlockedErrorVm>(
            "ErrorClassification should be Mo2BuildBlockedErrorVm when build is blocked by setting");

        var vm = (Mo2BuildBlockedErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Patcher State: {patcherRun.State.Value} (Failed: {patcherRun.State.Failed})");
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");

        vm.ErrorType.ShouldBe(Mo2BuildBlockedErrorClassification.ErrorTypeString);
        Output.WriteLine("Successfully verified build was blocked in Mo2 and classified as RanBuildInMo2");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based Mo2 blocked build error detection test
/// </summary>
public class Mo2BlockedBuildErrorCliPipelineTest : Mo2BlockedBuildErrorTest
{
    private Exception? _caughtException;

    public Mo2BlockedBuildErrorCliPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        // Use RunPatcherPipeline component directly with Mo2 detection and blocking enabled
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>(ConfigureMo2AndBlockSetting);

        // We expect this to throw or complete with an error
        try
        {
            await runPipeline.Run(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Expected - the build will be blocked
            _caughtException = ex;
            Output.WriteLine($"Run completed with expected error: {ex.Message}");
        }
    }

    protected override Task AssertErrorOccurred()
    {
        // Verify we caught an exception
        _caughtException.ShouldNotBeNull("Expected an exception to be thrown when build is blocked in Mo2");

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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(Mo2BuildBlockedErrorClassification.ErrorTypeString),
            "Should have logged the Mo2 error classification");

        Output.WriteLine("Successfully verified build was blocked in Mo2 and classified as RanBuildInMo2");
        return Task.CompletedTask;
    }
}
