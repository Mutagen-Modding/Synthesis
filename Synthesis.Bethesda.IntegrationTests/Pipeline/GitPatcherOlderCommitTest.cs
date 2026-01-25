using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Shouldly;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace Synthesis.Bethesda.IntegrationTests.Pipeline;

/// <summary>
/// Abstract base for Git patcher targeting an older commit tests
/// </summary>
public abstract class GitPatcherOlderCommitTest : IntegrationTest
{
    protected GitPatcherOlderCommitTest(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract override PipelineMode Mode { get; }

    [RetryFact(3)]
    public virtual async Task GitPatcher_TargetingOlderCommit_UsesCorrectVersion()
    {
        // Arrange

        // Create a test mod in the load order
        var testModKey = ModKey.FromNameAndExtension("TestMod.esp");
        CreateSimpleSkyrimMod(testModKey, mod =>
        {
            var npc = mod.Npcs.AddNew();
            npc.Name = "Integration Test NPC";
        });
        AddToLoadOrder(testModKey, enabled: true);

        // Create a Git patcher repository with initial commit
        var bareRepoPath = CreateGitPatcherRepositoryWithTwoCommits(
            "TestPatcherRepo",
            firstCommitNpcName: "First Commit NPC",
            secondCommitNpcName: "Second Commit NPC",
            out var firstCommitSha);

        // Export settings with patcher targeting the first commit by SHA
        var groupName = "Test Group";
        ExportSettingsWithPatchers(
            groupName: groupName,
            patchers: new[]
            {
                new GithubPatcherSettings
                {
                    On = true,
                    Nickname = "Test Patcher",
                    RemoteRepoPath = bareRepoPath,
                    SelectedProjectSubpath = "TestPatcher.csproj",
                    PatcherVersioning = PatcherVersioningEnum.Commit,
                    TargetCommit = firstCommitSha,
                    MutagenVersionType = PatcherNugetVersioningEnum.Profile,
                    SynthesisVersionType = PatcherNugetVersioningEnum.Profile
                }
            });

        // Act - Initialize and run
        await Act();

        // Assert - Check results
        await AssertNoErrors();

        // Verify output file exists
        var outputPath = Path.Combine(DataFolder, $"{groupName}.esp");
        File.Exists(outputPath).ShouldBeTrue("Output mod file should exist");

        // Verify we can load the output mod
        using var outputMod = SkyrimMod.CreateFromBinaryOverlay(outputPath, SkyrimRelease.SkyrimSE);
        var outputModKey = ModKey.FromNameAndExtension($"{groupName}.esp");
        outputMod.ModKey.ShouldBe(outputModKey);

        // Verify the patcher added the NPC with the name from the FIRST commit
        var addedNpc = outputMod.Npcs.FirstOrDefault(n => n.EditorID == "TestPatcherNPC");
        addedNpc.ShouldNotBeNull("Test NPC should exist in output");
        addedNpc.Name?.String.ShouldBe("First Commit NPC",
            "NPC should have the name from the first commit, not the second commit");
    }

    protected abstract Task Act();

    protected virtual Task AssertNoErrors()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a Git repository with two commits where the second commit changes an NPC name
    /// </summary>
    private string CreateGitPatcherRepositoryWithTwoCommits(
        string repositoryName,
        string firstCommitNpcName,
        string secondCommitNpcName,
        out string firstCommitSha)
    {
        // Create bare repository (acts as the "remote")
        var bareRepoPath = Path.Combine(TestFolder, $"{repositoryName}.git");
        Directory.CreateDirectory(bareRepoPath);

        Output.WriteLine($"Initializing bare repository at {bareRepoPath}");
        var initResult = RunGitCommand(bareRepoPath, "init --bare");
        if (initResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to initialize bare repository: {initResult.Error}");
        }

        // Create working directory for the patcher
        var workingDir = Path.Combine(TestFolder, $"{repositoryName}_working");
        Directory.CreateDirectory(workingDir);
        Output.WriteLine($"Created working directory {workingDir}");

        // Create the patcher project with first commit's NPC name
        CreateTestPatcherProjectInDirectory(
            workingDir,
            "TestPatcher",
            AddNpcToPatcher("TestPatcherNPC", firstCommitNpcName));

        // Initialize git repo in working directory
        var workingInitResult = RunGitCommand(workingDir, "init");
        if (workingInitResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to init git repo: {workingInitResult.Error}");
        }

        // Configure git user for the working directory (must be after git init)
        RunGitCommand(workingDir, "config user.email \"test@synthesis.com\"");
        RunGitCommand(workingDir, "config user.name \"Synthesis Test\"");

        // Add the bare repo as remote
        var remoteAddResult = RunGitCommand(workingDir, $"remote add origin \"{bareRepoPath}\"");
        if (remoteAddResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to add remote: {remoteAddResult.Error}");
        }

        // Add and commit all files (first commit)
        RunGitCommand(workingDir, "add .");
        var commitResult = RunGitCommand(workingDir, "commit -m \"First commit with initial NPC name\"");
        if (commitResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to commit: {commitResult.Error}");
        }

        // Get the SHA of the first commit
        var shaResult = RunGitCommand(workingDir, "rev-parse HEAD");
        if (shaResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get commit SHA: {shaResult.Error}");
        }
        firstCommitSha = shaResult.Output.Trim();
        Output.WriteLine($"First commit SHA: {firstCommitSha}");

        // Rename branch to master
        var branchResult = RunGitCommand(workingDir, "branch -M master");
        if (branchResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to rename branch: {branchResult.Error}");
        }

        // Push first commit to bare repo
        var pushResult = RunGitCommand(workingDir, "push -u origin master");
        if (pushResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to push: {pushResult.Error}");
        }

        // Now create a second commit with updated NPC name
        // Update the Program.cs file with the new NPC name
        CreateTestPatcherProjectInDirectory(
            workingDir,
            "TestPatcher",
            AddNpcToPatcher("TestPatcherNPC", secondCommitNpcName));

        // Commit the changes
        RunGitCommand(workingDir, "add .");
        var secondCommitResult = RunGitCommand(workingDir, "commit -m \"Second commit with updated NPC name\"");
        if (secondCommitResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to create second commit: {secondCommitResult.Error}");
        }

        // Get the SHA of the second commit for logging
        var secondShaResult = RunGitCommand(workingDir, "rev-parse HEAD");
        if (secondShaResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get second commit SHA: {secondShaResult.Error}");
        }
        var secondCommitSha = secondShaResult.Output.Trim();
        Output.WriteLine($"Second commit SHA: {secondCommitSha}");

        // Push second commit to bare repo
        var secondPushResult = RunGitCommand(workingDir, "push origin master");
        if (secondPushResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to push second commit: {secondPushResult.Error}");
        }

        // Set the default branch in the bare repo to master
        var headResult = RunGitCommand(bareRepoPath, "symbolic-ref HEAD refs/heads/master");
        if (headResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to set HEAD: {headResult.Error}");
        }

        Output.WriteLine($"Git patcher repository created with two commits at {bareRepoPath}");
        return bareRepoPath;
    }

    /// <summary>
    /// Helper to run a git command and return the result
    /// </summary>
    private (int ExitCode, string Output, string Error) RunGitCommand(string workingDirectory, string arguments)
    {
        return RunCommand(workingDirectory, "git", arguments);
    }

    /// <summary>
    /// Helper to run a command and return the result
    /// </summary>
    private (int ExitCode, string Output, string Error) RunCommand(string workingDirectory, string command, string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, output.ToString(), error.ToString());
    }

    /// <summary>
    /// Creates a minimal patcher project in a specific directory
    /// </summary>
    private void CreateTestPatcherProjectInDirectory(
        string projectFolder,
        string projectName,
        Action<GameRelease, Noggog.StructuredStrings.StructuredStringBuilder> generateRunPatchContent)
    {
        // Use Skyrim SE as the default game for tests
        var gameRelease = GameRelease.SkyrimSE;
        var gameNamespace = "Skyrim";

        // Get the Mutagen version from the currently loaded assembly
        var mutagenFullVersion = AssemblyVersions.For<FormKey>().ProductVersion
            ?? throw new InvalidOperationException("Could not determine Mutagen version");

        // Trim off git metadata (everything after '+')
        var mutagenVersion = mutagenFullVersion.Split('+')[0];

        // Get Synthesis version
        var synthesisVersion = GetSynthesisVersion();

        var csprojContent = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
                <ItemGroup>
                    <PackageReference Include="Mutagen.Bethesda" Version="{{mutagenVersion}}" />
                    <PackageReference Include="Mutagen.Bethesda.{{gameNamespace}}" Version="{{mutagenVersion}}" />
                    <PackageReference Include="Mutagen.Bethesda.Synthesis" Version="{{synthesisVersion}}" />
                </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectFolder, $"{projectName}.csproj"), csprojContent);

        // Generate the RunPatch method body content
        var runPatchBodyBuilder = new Noggog.StructuredStrings.StructuredStringBuilder();
        generateRunPatchContent(gameRelease, runPatchBodyBuilder);

        var programContent = $$"""
            using Mutagen.Bethesda.Synthesis;
            using Mutagen.Bethesda.{{gameNamespace}};

            return await SynthesisPipeline.Instance
                .AddPatch<I{{gameNamespace}}Mod, I{{gameNamespace}}ModGetter>(RunPatch)
                .Run(args);

            static void RunPatch(IPatcherState<I{{gameNamespace}}Mod, I{{gameNamespace}}ModGetter> state)
            {
            {{runPatchBodyBuilder}}
            }
            """;

        File.WriteAllText(Path.Combine(projectFolder, "Program.cs"), programContent);

        // Create solution file
        var projectGuid = Guid.NewGuid().ToString().ToUpper();
        var solutionContent = $$"""

            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{projectName}}", "{{projectName}}.csproj", "{{{projectGuid}}}"
            EndProject
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            		Release|Any CPU = Release|Any CPU
            	EndGlobalSection
            	GlobalSection(ProjectConfigurationPlatforms) = postSolution
            		{{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
            		{{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
            		{{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
            		{{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU
            	EndGlobalSection
            EndGlobal
            """;

        File.WriteAllText(Path.Combine(projectFolder, $"{projectName}.sln"), solutionContent);
    }
}

/// <summary>
/// UI-based Git patcher older commit test
/// </summary>
public class GitPatcherOlderCommitUIPipelineTest : GitPatcherOlderCommitTest
{
    public GitPatcherOlderCommitUIPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.UI;

    protected override async Task Act()
    {
        await RunPatcherPipeline();
    }

    protected override Task AssertNoErrors()
    {
        EnsureActiveRunHasNoErrors();
        return Task.CompletedTask;
    }
}

/// <summary>
/// CLI-based Git patcher older commit test
/// </summary>
public class GitPatcherOlderCommitCliPipelineTest : GitPatcherOlderCommitTest
{
    public GitPatcherOlderCommitCliPipelineTest(ITestOutputHelper output) : base(output)
    {
    }

    protected override PipelineMode Mode => PipelineMode.CLI;

    protected override async Task Act()
    {
        // Use RunPatcherPipeline component directly
        var runPipeline = GetComponentPayload<RunPatcherPipeline, RunPatcherPipeline>();
        await runPipeline.Run(CancellationToken.None);
    }
}
