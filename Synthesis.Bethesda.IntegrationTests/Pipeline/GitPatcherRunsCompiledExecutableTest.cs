using System.Reactive.Linq;
using Autofac;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ReactiveUI;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Synthesis.Bethesda.IntegrationTests.TestUtilities;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Abstract base for testing that Git patchers run compiled executables directly
/// instead of using `dotnet run --project`
/// </summary>
public abstract class GitPatcherRunsCompiledExecutableTest : IntegrationTest
{
    protected GitPatcherRunsCompiledExecutableTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    protected async Task TestRunsCompiledExecutableDirectly()
    {
        // Arrange - Create test mod and Git patcher
        var testModKey = ModKey.FromNameAndExtension("TestMod.esp");
        CreateSimpleSkyrimMod(testModKey, mod =>
        {
            var npc = mod.Npcs.AddNew();
            npc.Name = "Integration Test NPC";
        });
        AddToLoadOrder(testModKey, enabled: true);

        // Create a Git patcher repository
        var bareRepoPath = CreateGitPatcherRepository("TestPatcherRepo", AddTypicalPatcherNpc());
        var commitSha = GetLatestCommitSha(bareRepoPath);

        var groupName = "Test Group";
        var patchers = new[]
        {
            new GithubPatcherSettings
            {
                ID = "SomeID",
                On = true,
                Nickname = "Test Patcher",
                RemoteRepoPath = bareRepoPath,
                SelectedProjectSubpath = "TestPatcher.csproj",
                PatcherVersioning = PatcherVersioningEnum.Branch,
                TargetBranch = "master",
                TargetCommit = commitSha,
                MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                SynthesisVersionType = PatcherNugetVersioningEnum.Profile
            }
        };

        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: patchers);

        // Act - Run patcher
        await ActRun();

        // Assert - Run completed successfully
        await AssertNoErrors();

        var outputPath = Path.Combine(DataFolder, $"{groupName}.esp");
        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist after run");

        // Verify patcher ran correctly
        using (var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE))
        {
            var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
            addedNpc.ShouldNotBeNull("Test NPC should exist in output");
            addedNpc.Name?.String.ShouldBe("Test Patcher Was Here");
        }

        // Verify build meta was created with executable path
        var patcherId = patchers[0].ID;
        ProfileID.ShouldNotBeNullOrEmpty("ProfileID should be set after ExportSettingsWithPatchers");

        var profileDir = Path.Combine(TestFolder, "Temp", "Synthesis", ProfileID);
        var metaPath = Path.Combine(profileDir, "Git", patcherId, "Build.meta");
        File.Exists(metaPath).ShouldBeTrue($"Build.meta should exist at {metaPath}");

        var metaContent = File.ReadAllText(metaPath);
        var meta = System.Text.Json.JsonSerializer.Deserialize<GitCompilationMeta>(metaContent, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        meta.ShouldNotBeNull("Meta should deserialize successfully");
        meta.ExecutablePath.ShouldNotBeNull("ExecutablePath should be set in meta");
        File.Exists(meta.ExecutablePath).ShouldBeTrue($"Executable should exist at {meta.ExecutablePath}");

        // Assert - Verify that the patcher was run using the compiled executable directly
        // Check the captured logs for the execution command
        var logMessages = GetLogMessages();

        // Find log messages that indicate process execution
        // SynthesisSubProcessRunner logs: "({WorkingDirectory}): {FileName} {Args}"
        var allDotnetLogs = logMessages
            .Where(msg => msg.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Output.WriteLine("All dotnet-related logs:");
        foreach (var log in allDotnetLogs)
        {
            Output.WriteLine($"  {log}");
        }

        var processExecutionLogs = allDotnetLogs
            .Where(msg => !msg.Contains("build", StringComparison.OrdinalIgnoreCase))
            .Where(msg => !msg.Contains("restore", StringComparison.OrdinalIgnoreCase))
            .Where(msg => !msg.Contains("--version", StringComparison.OrdinalIgnoreCase))
            .Where(msg => !msg.Contains("--info", StringComparison.OrdinalIgnoreCase))
            .ToList();

        processExecutionLogs.ShouldNotBeEmpty("Should have logged at least one dotnet execution for running the patcher");

        Output.WriteLine("Process execution logs (filtered):");
        foreach (var log in processExecutionLogs)
        {
            Output.WriteLine($"  {log}");
        }

        // Verify that the patcher was run with the compiled DLL directly
        // Expected: "dotnet" followed by the DLL path
        var dllExecutionLog = processExecutionLogs.FirstOrDefault(log =>
            log.Contains(meta.ExecutablePath, StringComparison.OrdinalIgnoreCase));

        dllExecutionLog.ShouldNotBeNull(
            $"Should have found execution using compiled executable path: {meta.ExecutablePath}\n" +
            $"Process execution logs:\n{string.Join("\n", processExecutionLogs)}");

        // Verify that "dotnet run --project" was NOT used
        var runProjectLogs = logMessages
            .Where(log => log.Contains("dotnet", StringComparison.OrdinalIgnoreCase))
            .Where(log => log.Contains("run", StringComparison.OrdinalIgnoreCase))
            .Where(log => log.Contains("--project", StringComparison.OrdinalIgnoreCase))
            .ToList();

        runProjectLogs.ShouldBeEmpty(
            $"Git patcher should NOT be run with 'dotnet run --project'. Found:\n" +
            $"{string.Join("\n", runProjectLogs)}");

        Output.WriteLine($"✓ Verified patcher ran using compiled executable: {meta.ExecutablePath}");
        Output.WriteLine($"✓ Verified 'dotnet run --project' was NOT used");
    }

    protected abstract Task ActRun();

    protected virtual Task AssertNoErrors()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets log messages for assertion. Override in derived classes to provide
    /// mode-specific log capture (e.g., from IReporterLoggerWrapper.Events in UI mode).
    /// </summary>
    protected virtual IReadOnlyList<string> GetLogMessages()
    {
        return LogSink.Messages;
    }
}

/// <summary>
/// UI test - verifies Git patchers run compiled executable directly
/// </summary>
public class GitPatcherRunsCompiledExecutableUiTest : GitPatcherRunsCompiledExecutableTest
{
    private List<string> _patcherOutputMessages = new();

    public GitPatcherRunsCompiledExecutableUiTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    [RetryFact(3)]
    public async Task GitPatcher_RunsCompiledExecutableDirectly()
    {
        await TestRunsCompiledExecutableDirectly();
    }

    protected override async Task ActRun()
    {
        await GetComponentPayloadAndInitialize<EmptyPayload>();

        var payload = GetStoredPayload();

        // Execute RunPatchers command
        Output.WriteLine("Executing RunPatchers command...");
        await Observable.Start(() =>
        {
            payload.ProfileManager.RunPatchers.Execute().Subscribe();
        }, payload.SchedulerProvider.MainThread);

        // Wait for run to complete
        payload.ActiveRunVm.CurrentRun.ShouldNotBeNull("CurrentRun should be set after executing RunPatchers");
        Output.WriteLine("Waiting for run to complete...");
        await payload.ActiveRunVm.CurrentRun.WhenAnyValue(x => x.Running)
            .Where(running => !running)
            .FirstAsync()
            .Timeout(TimeSpan.FromMinutes(2));

        Output.WriteLine("Run completed");

        // Capture log messages from IReporterLoggerWrapper.Events via PatcherRunVm.OutputDisplay
        // In UI mode, logs go through ReporterLoggerWrapper which emits to Events,
        // and PatcherRunVm subscribes to those events and accumulates them in OutputDisplay
        _patcherOutputMessages = payload.ActiveRunVm.CurrentRun.Groups
            .SelectMany(g => g.Patchers)
            .Select(p => p.OutputDisplay.Text)
            .SelectMany(text => text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        Output.WriteLine($"Captured {_patcherOutputMessages.Count} log messages from patcher output");
    }

    protected override Task AssertNoErrors()
    {
        EnsureActiveRunHasNoErrors();
        return Task.CompletedTask;
    }

    /// <summary>
    /// In UI mode, logs are captured from IReporterLoggerWrapper.Events via PatcherRunVm.OutputDisplay.
    /// We combine both the patcher output and the standard LogSink messages.
    /// </summary>
    protected override IReadOnlyList<string> GetLogMessages()
    {
        // Combine messages from both sources - LogSink captures non-patcher logs,
        // while patcher output captures logs from ReporterLoggerWrapper.Events
        return LogSink.Messages.Concat(_patcherOutputMessages).ToList();
    }
}

/// <summary>
/// CLI test - verifies Git patchers run compiled executable directly
/// </summary>
public class GitPatcherRunsCompiledExecutableCliTest : GitPatcherRunsCompiledExecutableTest
{
    public GitPatcherRunsCompiledExecutableCliTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    [RetryFact(3)]
    public async Task GitPatcher_RunsCompiledExecutableDirectly()
    {
        await TestRunsCompiledExecutableDirectly();
    }

    protected override async Task ActRun()
    {
        var runPipeline = GetComponentPayload<RunPatcherPipeline, object>();
        await runPipeline.Run(CancellationToken.None);
    }
}
