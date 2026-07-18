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
/// Abstract base for .NET runtime missing error detection tests.
/// Tests that we detect "You must install or update .NET to run this application" errors
/// and extract the required framework version.
/// </summary>
public abstract class DotNetRuntimeMissingErrorTest : IntegrationTest
{
    protected DotNetRuntimeMissingErrorTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [Fact]
    public async Task DotNetRuntimeMissingError_IsDetectedAndReported()
    {
        // Arrange
        var patcher = CreateSolutionPatcherWithSettings(
            "DotNetRuntimeMissingPatcher",
            GenerateDotNetRuntimeMissingPatchContent(),
            nickname: "DotNet Runtime Missing Patcher");

        ExportSettingsWithPatchers(
            groupName: "Test Group",
            patchers: new[] { patcher });

        // Act
        await Act();

        // Assert
        await AssertErrorOccurred();
    }

    /// <summary>
    /// Generates patcher content that throws an exception matching the .NET runtime missing pattern
    /// </summary>
    private static Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> GenerateDotNetRuntimeMissingPatchContent()
    {
        return (gameRelease, sb) =>
        {
            sb.AppendLine("// Simulate .NET runtime missing error");
            sb.AppendLine("throw new System.Exception(");
            sb.AppendLine("    \"You must install or update .NET to run this application.\\n\\n\" +");
            sb.AppendLine("    \"Framework: 'Microsoft.NETCore.App', version '9.0.0' (x64)\\n\" +");
            sb.AppendLine("    \".NET location: C:\\\\Program Files\\\\dotnet\\\\\\n\\n\" +");
            sb.AppendLine("    \"The following frameworks were found:\\n\" +");
            sb.AppendLine("    \"  10.0.5 at [C:\\\\Program Files\\\\dotnet\\\\shared\\\\Microsoft.NETCore.App]\");");
        };
    }

    protected abstract Task Act();

    protected virtual Task AssertErrorOccurred()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// UI-based .NET runtime missing error detection test
/// </summary>
public class DotNetRuntimeMissingErrorUIPipelineTest : DotNetRuntimeMissingErrorTest
{
    public DotNetRuntimeMissingErrorUIPipelineTest(ITestOutputHelper output) : base(output)
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
        patcherRun.ErrorClassification.ShouldBeOfType<DotNetRuntimeMissingErrorVm>(
            "ErrorClassification should be DotNetRuntimeMissingErrorVm");

        var vm = (DotNetRuntimeMissingErrorVm)patcherRun.ErrorClassification;
        Output.WriteLine($"Error Classification Type: {vm.ErrorType}");
        Output.WriteLine($"Error Message: {vm.Message}");
        Output.WriteLine($"Required Version: {vm.RequiredVersion}");
        vm.ErrorType.ShouldBe(DotNetRuntimeMissingErrorClassification.ErrorTypeString);
        vm.RequiredVersion.ShouldBe("9.0.0", "Should have extracted the required .NET version");
        Output.WriteLine("Successfully verified .NET runtime missing error was detected and classified");

        await Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based .NET runtime missing error detection test
/// </summary>
public class DotNetRuntimeMissingErrorCliPipelineTest : DotNetRuntimeMissingErrorTest
{
    private Exception? _caughtException;

    public DotNetRuntimeMissingErrorCliPipelineTest(ITestOutputHelper output) : base(output)
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
        errorMessages.ShouldContain(msg => msg.Contains("Error detected:") && msg.Contains(DotNetRuntimeMissingErrorClassification.ErrorTypeString),
            "Should have logged the .NET runtime missing error classification");
        errorMessages.ShouldContain(msg => msg.Contains("Runtime is not the same as the .NET SDK") || msg.Contains("runtime version"),
            "Should have logged the error suggestion");

        Output.WriteLine("Successfully verified .NET runtime missing error was detected and classified");
        return Task.CompletedTask;
    }
}
