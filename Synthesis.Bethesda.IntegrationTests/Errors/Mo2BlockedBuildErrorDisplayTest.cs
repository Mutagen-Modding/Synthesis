using System.Reactive.Linq;
using Autofac;
using Mutagen.Bethesda;
using ReactiveUI;
using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Reporters.Classifications;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Errors;
using Synthesis.Bethesda.IntegrationTests.Components;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Errors;

/// <summary>
/// Tests that the ErrorDisplayVm correctly shows the error classification title
/// during the configuration phase (when compilation fails), not just during the run phase.
///
/// This test was added to catch a bug where:
/// - ErrorDisplayVm.GoToErrorCommand always set DisplayedObject = ErrorVM
/// - This overwrote the classified VM with the plain ErrorVM showing "Compiling"
/// - The fix tracks _currentErrorObject and uses that in the command
/// - Also added ErrorTitle property to show classification title in BottomErrorDisplayView
/// </summary>
public class Mo2BlockedBuildErrorDisplayTest : IntegrationTest
{
    public Mo2BlockedBuildErrorDisplayTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [Fact]
    public async Task ErrorDisplayVm_ShowsClassificationTitle_WhenBuildBlockedInMo2()
    {
        // Arrange - Create a patcher that will fail to build due to Mo2 blocking
        var patcher = CreateGitPatcherWithSettings(
            "Mo2ErrorDisplayPatcher",
            AddTypicalPatcherNpc(),
            nickname: "Mo2 Error Display Test Patcher");

        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[] { patcher });

        // Act - Initialize with Mo2 detection and blocking enabled, but don't wait for success
        var payload = GetComponentPayload<InitializationPayload<EmptyPayload>, EmptyPayload>(ConfigureMo2AndBlockSetting);

        // Start initialization
        Output.WriteLine("Starting initialization...");
        var startupTask = payload.Startup.Initialize();

        // Wait for startup tracker to be initialized
        await payload.StartupTracker.WhenAnyValue(x => x.Initialized)
            .Where(initialized => initialized)
            .FirstAsync()
            .Timeout(TestTimeouts.Base);

        await startupTask;

        // Let throttles settle
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Wait for profile to be selected
        Output.WriteLine("Waiting for profile...");
        var selectedProfile = await payload.ProfileManager.WhenAnyValue(x => x.SelectedProfile)
            .Where(p => p != null)
            .FirstAsync()
            .Timeout(TestTimeouts.Short);

        selectedProfile.ShouldNotBeNull();

        // Flush dispatcher
        await Observable.Return(System.Reactive.Unit.Default)
            .ObserveOn(payload.SchedulerProvider.MainThread)
            .FirstAsync();

        // Wait for the patcher state to fail (build will be blocked)
        Output.WriteLine("Waiting for patcher to reach error state...");
        var gitPatcher = selectedProfile.Groups.Items
            .SelectMany(g => g.Patchers.Items)
            .OfType<GitPatcherVm>()
            .FirstOrDefault();

        gitPatcher.ShouldNotBeNull("Should have a Git patcher");

        // Wait for state to be halting error (build blocked)
        await gitPatcher.WhenAnyValue(x => x.State)
            .Where(state => state.IsHaltingError)
            .FirstAsync()
            .Timeout(TestTimeouts.Long);

        Output.WriteLine($"Patcher state: {gitPatcher.State.RunnableState.Reason}");

        // Assert - Check ErrorDisplayVm properties
        var errorDisplayVm = gitPatcher.ErrorDisplayVm;

        // The ErrorTitle should be the classification title, not the exception type name
        Output.WriteLine($"ErrorTitle: {errorDisplayVm.ErrorTitle}");
        errorDisplayVm.ErrorTitle.ShouldBe(Mo2BuildBlockedErrorClassification.ErrorTypeString,
            "ErrorTitle should show the classification title, not the exception type");

        // Execute GoToErrorCommand to simulate user clicking the error
        Output.WriteLine("Executing GoToErrorCommand...");
        await Observable.Start(() =>
        {
            if (errorDisplayVm.GoToErrorCommand.CanExecute(null))
            {
                errorDisplayVm.GoToErrorCommand.Execute(null);
            }
        }, payload.SchedulerProvider.MainThread);

        // Flush dispatcher
        await Observable.Return(System.Reactive.Unit.Default)
            .ObserveOn(payload.SchedulerProvider.MainThread)
            .FirstAsync();

        // The DisplayedObject should be a Mo2BuildBlockedErrorVm, not the plain ErrorVM
        Output.WriteLine($"DisplayedObject type: {errorDisplayVm.DisplayedObject?.GetType().Name}");
        errorDisplayVm.DisplayedObject.ShouldBeOfType<Mo2BuildBlockedErrorVm>(
            "After clicking the error, DisplayedObject should be Mo2BuildBlockedErrorVm, not ErrorVM");

        var classifiedVm = (Mo2BuildBlockedErrorVm)errorDisplayVm.DisplayedObject;
        classifiedVm.ErrorType.ShouldBe(Mo2BuildBlockedErrorClassification.ErrorTypeString);

        Output.WriteLine("Successfully verified ErrorDisplayVm shows classification correctly during config phase");
    }

    /// <summary>
    /// Configures the container to mock Mo2 environment detection and enable BlockBuildingWithinMo2
    /// </summary>
    private void ConfigureMo2AndBlockSetting(ContainerBuilder builder)
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
