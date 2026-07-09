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
/// Abstract base for outdated .NET SDK error detection tests.
/// Tests that we detect NETSDK1045 "The current .NET SDK does not support targeting .NET X.Y" errors
/// and extract the targeted framework version.
/// </summary>
public abstract class DotNetSdkOutdatedErrorTest : IntegrationTest
{
    protected DotNetSdkOutdatedErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task DotNetSdkOutdatedError_IsDetectedAndReported()
    {
        // Arrange
        var patcher = CreateSolutionPatcherWithSettings(
            "DotNetSdkOutdatedPatcher",
            GenerateDotNetSdkOutdatedPatchContent(),
            nickname: "DotNet SDK Outdated Patcher");

        ExportSettingsWithPatchers(
            groupName: "Test Group",
            patchers: new[] { patcher });

        // Act
        await Act();

        // Assert
        await AssertErrorOccurred();
    }

    /// <summary>
    /// Generates patcher content that throws an exception matching the outdated-SDK pattern
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateDotNetSdkOutdatedPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate an outdated .NET SDK build error (NETSDK1045)");
            sb.AppendLine("throw new System.Exception(");
            sb.AppendLine("    \"error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.  \" +");
            sb.AppendLine("    \"Either target .NET 8.0 or lower, or use a version of the .NET SDK that supports .NET 9.0.\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based outdated .NET SDK error detection test
/// </summary>
public class DotNetSdkOutdatedErrorUIPipelineTest : DotNetSdkOutdatedErrorTest
{
    public DotNetSdkOutdatedErrorUIPipelineTest(ITestOutputHelper output) : base(output)
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
        patcherRun.ErrorClassification.ShouldBeOfType<DotNetSdkOutdatedErrorVm>(
            "ErrorClassification should be DotNetSdkOutdatedErrorVm");

        var vm = (DotNetSdkOutdatedErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        Output.WriteLine($"Target Version: {vm.TargetVersion}");
        vm.ErrorType.ShouldBe(DotNetSdkOutdatedErrorClassification.ErrorTypeString);
        vm.TargetVersion.ShouldBe("9.0", "Should have extracted the targeted .NET version");
        Output.WriteLine("Successfully verified outdated .NET SDK error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based outdated .NET SDK error detection test
/// </summary>
public class DotNetSdkOutdatedErrorCliPipelineTest : DotNetSdkOutdatedErrorTest
{
    private Exception? _caughtException;

    public DotNetSdkOutdatedErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(DotNetSdkOutdatedErrorClassification.ErrorTypeString),
            "Should have logged the outdated .NET SDK error classification");
        errorMessages.ShouldContain(msg => msg.Contains("Install the latest .NET SDK"),
            "Should have logged the error suggestion");

        Output.WriteLine("Successfully verified outdated .NET SDK error was detected and classified");
        return Task.CompletedTask;
    }
}
