using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Autofac;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis.CLI;
using Newtonsoft.Json;
using Noggog;
using Noggog.IO;
using Noggog.Reactive;
using Noggog.StructuredStrings;
using ReactiveUI;
using Shouldly;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.Logging;
using Synthesis.Bethesda.GUI.Services.Startup;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Top;
using Xunit.Abstractions;

namespace Synthesis.Bethesda.IntegrationTests.Infrastructure;

/// <summary>
/// Represents a NuGet package reference to add to a test patcher project
/// </summary>
/// <param name="PackageId">The NuGet package ID</param>
/// <param name="Version">The package version</param>
public record PackageReference(string PackageId, string Version);

/// <summary>
/// Abstract base class for integration tests that provides temp directory management and test utilities
/// </summary>
public abstract class IntegrationTest : IDisposable
{
    private const string OverallTempFolderPath = "SynthesisIntegrationTests";
    private readonly TempFolder _tempFolder;
    private readonly CompositeDisposable _disposable = new();

    // Internal payload stored for reuse in helper methods
    private IInitializationPayload? _internalPayload;

    // Log sink for capturing log messages during test execution
    public TestUtilities.CapturingLogSink LogSink { get; }

    public DirectoryPath TestFolder => _tempFolder.Dir;
    public DirectoryPath SynthesisRepoRoot { get; }
    public DirectoryPath DataFolder { get; private set; }
    public FilePath PluginsPath { get; private set; }
    public ITestOutputHelper Output { get; }

    /// <summary>
    /// Pipeline execution mode - must be specified by derived test classes
    /// </summary>
    protected abstract PipelineMode Mode { get; }

    protected IntegrationTest(ITestOutputHelper output)
    {
        Output = output;

        // Initialize log sink for capturing log messages during test execution
        LogSink = new TestUtilities.CapturingLogSink();

        // Set logging to testing mode to avoid file I/O
        LogPreferences.IsTesting = true;

        // Create temp folder named after the test class
        var testClassName = GetType().Name;
        _tempFolder = TempFolder.FactoryByAddedPath(
            Path.Combine(OverallTempFolderPath, testClassName),
            throwIfUnsuccessfulDisposal: false,
            deleteBefore: true,
            deleteAfter: true);
        _disposable.Add(_tempFolder);

        // Calculate repo root from assembly location
        // Test runner sets current directory to bin output folder, so we navigate up from there
        // Can be either bin/Debug/net9.0 (4 levels) or bin/x64/Debug/net9.0 (5 levels)
        var currentDir = Directory.GetCurrentDirectory();
        if (currentDir.Contains($"{Path.DirectorySeparatorChar}x64{Path.DirectorySeparatorChar}"))
        {
            // bin/x64/Debug/net9.0 -> go up 5 levels to reach repo root
            SynthesisRepoRoot = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", "..", ".."));
        }
        else
        {
            // bin/Debug/net9.0 -> go up 4 levels to reach repo root
            SynthesisRepoRoot = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
        }

        // Verify we found the correct location
        var synthesisProjectPath = Path.Combine(SynthesisRepoRoot, "Mutagen.Bethesda.Synthesis", "Mutagen.Bethesda.Synthesis.csproj");
        if (!File.Exists(synthesisProjectPath))
        {
            throw new InvalidOperationException(
                $"Cannot find Mutagen.Bethesda.Synthesis project. Calculated wrong repo root.\n" +
                $"Current directory: {currentDir}\n" +
                $"Calculated repo root: {SynthesisRepoRoot}\n" +
                $"Expected project at: {synthesisProjectPath}\n" +
                $"Please ensure tests are run with 'dotnet test' from the repository root.");
        }

        CreateSkyrimEnvironment();
    }

    /// <summary>
    /// Gets the Synthesis version from the currently loaded assembly
    /// </summary>
    protected string GetSynthesisVersion()
    {
        var synthesisFullVersion = AssemblyVersions.For<Mutagen.Bethesda.Synthesis.SynthesisPipeline>().ProductVersion
            ?? throw new InvalidOperationException("Could not determine Synthesis version");
        return synthesisFullVersion.Split('+')[0];
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }

    /// <summary>
    /// Creates a basic Skyrim SE game environment with data folder and load order
    /// Returns (DataFolder, PluginsPath)
    /// </summary>
    protected void CreateSkyrimEnvironment()
    {
        var dataFolder = Path.Combine(TestFolder, "Data");
        Directory.CreateDirectory(dataFolder);

        // Create Skyrim.esm (required master)
        var skyrimMod = new SkyrimMod(ModKey.FromNameAndExtension("Skyrim.esm"), SkyrimRelease.SkyrimSE);
        skyrimMod.BeginWrite
            .IntoFolder(dataFolder)
            .WithNoLoadOrder()
            .NoModKeySync()
            .Write();

        // Create plugins.txt
        var pluginsPath = Path.Combine(TestFolder, "Plugins.txt");
        File.WriteAllLines(pluginsPath, new[] { "*Skyrim.esm" });

        DataFolder = dataFolder;
        PluginsPath = pluginsPath;
    }

    /// <summary>
    /// Creates a simple Skyrim mod with minimal content
    /// </summary>
    public void CreateSimpleSkyrimMod(ModKey modKey, Action<ISkyrimMod>? configure = null)
    {
        var mod = new SkyrimMod(modKey, SkyrimRelease.SkyrimSE);

        // Add some simple content so it's not completely empty
        var npc = mod.Npcs.AddNew();
        npc.Name = "Test NPC";

        configure?.Invoke(mod);

        mod.BeginWrite
            .ToPath(Path.Combine(DataFolder, modKey.FileName))
            .WithNoLoadOrder()
            .NoModKeySync()
            .Write();
    }

    /// <summary>
    /// Adds a mod to a plugins.txt file
    /// </summary>
    public void AddToLoadOrder(ModKey modKey, bool enabled = true)
    {
        var prefix = enabled ? "*" : "";
        File.AppendAllText(PluginsPath, $"{prefix}{modKey.FileName}{Environment.NewLine}");
    }

    /// <summary>
    /// Convenience method to generate RunPatch content that adds a typical test NPC.
    /// This is the most common test scenario - adds an NPC with default test values.
    /// </summary>
    public static Action<GameRelease, StructuredStringBuilder> AddTypicalPatcherNpc()
    {
        return AddNpcToPatcher("TestPatcherNPC", "Test Patcher Was Here");
    }

    /// <summary>
    /// Generates RunPatch method body content that adds a specific NPC to the patch.
    /// Can be passed as the generateRunPatchContent callback to CreateTestPatcherProjectInDirectory.
    /// </summary>
    public static Action<GameRelease, StructuredStringBuilder> AddNpcToPatcher(string npcEditorId, string npcName)
    {
        return (gameRelease, sb) =>
        {
            var gameNamespace = GetGameNamespace(gameRelease);
            sb.AppendLine("// Simple test patcher - just outputs some info");
            sb.AppendLine("Console.WriteLine($\"Test patcher running - Patch ModKey: {state.PatchMod.ModKey}\");");
            sb.AppendLine("Console.WriteLine($\"Load Order count: {state.LoadOrder.Count}\");");
            sb.AppendLine("Console.WriteLine($\"Data folder: {state.DataFolderPath}\");");
            sb.AppendLine();
            sb.AppendLine("// Add a simple record to verify patcher actually modifies the patch");
            sb.AppendLine($"var npc = new Npc(state.PatchMod.GetNextFormKey(), {gameNamespace}Release.{gameNamespace}SE);");
            sb.AppendLine($"npc.EditorID = \"{npcEditorId}\";");
            sb.AppendLine($"npc.Name = \"{npcName}\";");
            sb.AppendLine("state.PatchMod.Npcs.Add(npc);");
            sb.AppendLine();
            sb.AppendLine("Console.WriteLine(\"Test patcher completed successfully\");");
        };
    }

    /// <summary>
    /// Creates a Git repository with a patcher project for testing Git patchers.
    /// Returns the path to the bare repository that can be used as a Git remote.
    /// </summary>
    /// <param name="repositoryName">Name for the repository (will be {name}.git for bare repo)</param>
    /// <param name="projectName">Name for the C# project</param>
    /// <param name="generateRunPatchContent">Optional callback to generate the RunPatch method body content. If null, uses default NPC-adding implementation.</param>
    /// <param name="additionalPackageReferences">Optional additional package references to include in the project</param>
    public DirectoryPath CreateGitPatcherRepository(
        string repositoryName,
        Action<GameRelease, StructuredStringBuilder> generateRunPatchContent,
        string projectName = "TestPatcher",
        IEnumerable<PackageReference>? additionalPackageReferences = null)
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

        // Create the patcher project in the working directory
        CreateTestPatcherProjectInDirectory(workingDir, projectName, generateRunPatchContent, additionalPackageReferences);

        // Configure git user for the working directory
        RunGitCommand(workingDir, "config user.email \"test@synthesis.com\"");
        RunGitCommand(workingDir, "config user.name \"Synthesis Test\"");

        // Initialize git repo in working directory (since clone of empty bare repo doesn't create a branch)
        var workingInitResult = RunGitCommand(workingDir, "init");
        if (workingInitResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to init git repo: {workingInitResult.Error}");
        }

        // Add the bare repo as remote
        var remoteAddResult = RunGitCommand(workingDir, $"remote add origin \"{bareRepoPath}\"");
        if (remoteAddResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to add remote: {remoteAddResult.Error}");
        }

        // Add and commit all files
        RunGitCommand(workingDir, "add .");
        var commitResult = RunGitCommand(workingDir, "commit -m \"Initial patcher commit\"");
        if (commitResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to commit: {commitResult.Error}");
        }

        // Rename branch to master (for consistency with test expectations)
        var branchResult = RunGitCommand(workingDir, "branch -M master");
        if (branchResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to rename branch: {branchResult.Error}");
        }

        // Push to bare repo
        var pushResult = RunGitCommand(workingDir, "push -u origin master");
        if (pushResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to push: {pushResult.Error}");
        }

        // Set the default branch in the bare repo to master
        var headResult = RunGitCommand(bareRepoPath, "symbolic-ref HEAD refs/heads/master");
        if (headResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to set HEAD: {headResult.Error}");
        }

        Output.WriteLine($"Git patcher repository created at {bareRepoPath}");
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
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
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
    /// Gets the latest commit SHA from a Git repository
    /// </summary>
    public string GetLatestCommitSha(DirectoryPath repoPath)
    {
        var result = RunGitCommand(repoPath, "rev-parse HEAD");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get commit SHA: {result.Error}");
        }
        return result.Output.Trim();
    }

    /// <summary>
    /// Creates a Git patcher repository and returns typical GithubPatcherSettings configured to use it.
    /// This is a convenience method that combines CreateGitPatcherRepository with typical settings configuration.
    /// </summary>
    /// <param name="repositoryName">Name for the repository (will be {name}.git for bare repo)</param>
    /// <param name="generateRunPatchContent">Callback to generate the RunPatch method body content</param>
    /// <param name="nickname">Optional nickname for the patcher (defaults to repositoryName)</param>
    /// <param name="projectName">Name for the C# project (defaults to "TestPatcher")</param>
    /// <param name="additionalPackageReferences">Optional additional package references to include in the project</param>
    /// <returns>Configured GithubPatcherSettings ready to use in tests</returns>
    public GithubPatcherSettings CreateGitPatcherWithSettings(
        string repositoryName,
        Action<GameRelease, StructuredStringBuilder> generateRunPatchContent,
        string? nickname = null,
        string projectName = "TestPatcher",
        IEnumerable<PackageReference>? additionalPackageReferences = null)
    {
        var bareRepoPath = CreateGitPatcherRepository(repositoryName, generateRunPatchContent, projectName, additionalPackageReferences);
        var commitSha = GetLatestCommitSha(bareRepoPath);

        return new GithubPatcherSettings
        {
            On = true,
            ID = Path.GetRandomFileName(),
            Nickname = nickname ?? repositoryName,
            RemoteRepoPath = bareRepoPath,
            SelectedProjectSubpath = $"{projectName}.csproj",
            PatcherVersioning = PatcherVersioningEnum.Commit,
            TargetCommit = commitSha,
            MutagenVersionType = PatcherNugetVersioningEnum.Profile,
            SynthesisVersionType = PatcherNugetVersioningEnum.Profile
        };
    }

    /// <summary>
    /// Creates a solution patcher project and returns typical SolutionPatcherSettings configured to use it.
    /// This is a convenience method that combines CreateTestPatcherProject with typical settings configuration.
    /// </summary>
    /// <param name="projectName">Name for the C# project</param>
    /// <param name="generateRunPatchContent">Callback to generate the RunPatch method body content</param>
    /// <param name="nickname">Optional nickname for the patcher (defaults to projectName)</param>
    /// <param name="additionalPackageReferences">Optional additional package references to include in the project</param>
    /// <returns>Configured SolutionPatcherSettings ready to use in tests</returns>
    public SolutionPatcherSettings CreateSolutionPatcherWithSettings(
        string projectName,
        Action<GameRelease, StructuredStringBuilder> generateRunPatchContent,
        string? nickname = null,
        IEnumerable<PackageReference>? additionalPackageReferences = null)
    {
        var projectFolder = CreateTestPatcherProject(projectName, generateRunPatchContent, additionalPackageReferences);

        return new SolutionPatcherSettings
        {
            On = true,
            Nickname = nickname ?? projectName,
            SolutionPath = Path.Combine(projectFolder, $"{projectName}.sln"),
            ProjectSubpath = $"{projectName}.csproj"
        };
    }

    /// <summary>
    /// Creates a tag in a Git repository pointing to the current HEAD.
    /// For bare repositories, this finds the working directory and creates the tag there, then pushes it.
    /// </summary>
    public void CreateTag(DirectoryPath repoPath, string tagName)
    {
        var repoPathStr = repoPath.Path;

        // Check if this is a bare repository
        if (repoPathStr.EndsWith(".git"))
        {
            // Find the working directory
            var workingDir = repoPathStr.Replace(".git", "_working");
            if (!Directory.Exists(workingDir))
            {
                throw new InvalidOperationException($"Working directory not found at {workingDir}");
            }

            // Create tag in working directory
            var tagResult = RunGitCommand(workingDir, $"tag {tagName}");
            if (tagResult.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to create tag: {tagResult.Error}");
            }

            // Push the tag to the bare repo
            var pushResult = RunGitCommand(workingDir, $"push origin {tagName}");
            if (pushResult.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to push tag: {pushResult.Error}");
            }

            Output.WriteLine($"Created and pushed tag '{tagName}' to repository at {repoPath}");
        }
        else
        {
            // Regular repository - just create the tag
            var result = RunGitCommand(repoPath, $"tag {tagName}");
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to create tag: {result.Error}");
            }
            Output.WriteLine($"Created tag '{tagName}' in repository at {repoPath}");
        }
    }

    /// <summary>
    /// Creates a minimal patcher project in a specific directory
    /// </summary>
    private void CreateTestPatcherProjectInDirectory(
        string projectFolder,
        string projectName,
        Action<GameRelease, StructuredStringBuilder> generateRunPatchContent,
        IEnumerable<PackageReference>? additionalPackageReferences = null)
    {

        // Use Skyrim SE as the default game for tests
        var gameRelease = GameRelease.SkyrimSE;
        var gameNamespace = GetGameNamespace(gameRelease);

        // Get the Mutagen version from the currently loaded assembly
        var mutagenFullVersion = AssemblyVersions.For<FormKey>().ProductVersion
            ?? throw new InvalidOperationException("Could not determine Mutagen version");

        // Trim off git metadata (everything after '+')
        var mutagenVersion = mutagenFullVersion.Split('+')[0];

        // Get Synthesis version
        var synthesisVersion = GetSynthesisVersion();

        // Build the package references
        var packageReferencesBuilder = new StructuredStringBuilder();
        packageReferencesBuilder.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda\" Version=\"{mutagenVersion}\" />");
        packageReferencesBuilder.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda.{gameNamespace}\" Version=\"{mutagenVersion}\" />");
        packageReferencesBuilder.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda.Synthesis\" Version=\"{synthesisVersion}\" />");

        if (additionalPackageReferences != null)
        {
            foreach (var packageRef in additionalPackageReferences)
            {
                packageReferencesBuilder.AppendLine($"<PackageReference Include=\"{packageRef.PackageId}\" Version=\"{packageRef.Version}\" />");
            }
        }

        var csprojContent = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net9.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
                <ItemGroup>
            {{packageReferencesBuilder}}    </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectFolder, $"{projectName}.csproj"), csprojContent);

        // Generate the RunPatch method body content
        var runPatchBodyBuilder = new StructuredStringBuilder();
        generateRunPatchContent(gameRelease, runPatchBodyBuilder);

        var programContent = $$"""
            using Mutagen.Bethesda;
            using Mutagen.Bethesda.Plugins;
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

    /// <summary>
    /// Creates a minimal patcher project for testing
    /// </summary>
    /// <param name="projectName">Name for the project</param>
    /// <param name="generateRunPatchContent">Optional callback to generate the RunPatch method body content. If null, uses default NPC-adding implementation.</param>
    /// <param name="additionalPackageReferences">Optional additional package references to include in the project</param>
    public DirectoryPath CreateTestPatcherProject(
        string projectName,
        Action<GameRelease, StructuredStringBuilder> generateRunPatchContent,
        IEnumerable<PackageReference>? additionalPackageReferences = null)
    {
        var projectFolder = Path.Combine(TestFolder, projectName);
        Directory.CreateDirectory(projectFolder);
        CreateTestPatcherProjectInDirectory(projectFolder, projectName, generateRunPatchContent, additionalPackageReferences);

        return projectFolder;
    }

    private static string GetGameNamespace(GameRelease release)
    {
        // For now, only support Skyrim in integration tests
        if (release.ToCategory() != GameCategory.Skyrim)
        {
            throw new ArgumentException($"Integration tests currently only support Skyrim. Got: {release}");
        }
        return "Skyrim";
    }

    /// <summary>
    /// Builds a patcher project
    /// </summary>
    public async Task<ProcessResult> BuildPatcher(DirectoryPath projectFolder)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectFolder}\" -c Debug",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start build process");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = output + "\n" + error
        };
    }

    /// <summary>
    /// Runs a patcher with the given options
    /// </summary>
    public async Task<ProcessResult> RunPatcher(FilePath patcherDll, RunSynthesisMutagenPatcher args)
    {
        var formatter = new FormatCommandLine();
        var arguments = formatter.Format(args);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{patcherDll}\" run-patcher {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start patcher process");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            Output = output + "\n" + error
        };
    }

    /// <summary>
    /// Result from running a process
    /// </summary>
    public record ProcessResult
    {
        public required int ExitCode { get; init; }
        public required string Output { get; init; }
    }

    /// <summary>
    /// Exports pipeline and GUI settings with the specified patchers
    /// </summary>
    /// <param name="groupName">Name of the patcher group</param>
    /// <param name="patchers">List of patcher settings to include</param>
    /// <param name="profileNickname">Optional profile nickname (defaults to "Test Profile")</param>
    /// <param name="gameRelease">Optional game release (defaults to SkyrimSE)</param>
    /// <returns>The profile ID that was created</returns>
    public string ExportSettingsWithPatchers(
        string groupName,
        IEnumerable<PatcherSettings> patchers,
        string? profileNickname = null,
        GameRelease? gameRelease = null,
        bool splitIfMaxMastersExceeded = true)
    {
        var profileId = Path.GetRandomFileName();

        // Get the Mutagen version from the currently loaded assembly
        var mutagenFullVersion = AssemblyVersions.For<FormKey>().ProductVersion
            ?? throw new InvalidOperationException("Could not determine Mutagen version");

        // Trim off git metadata (everything after '+')
        var mutagenVersion = mutagenFullVersion.Split('+')[0];

        // Get Synthesis version
        var synthesisVersion = GetSynthesisVersion();

        var pipelineSettings = new PipelineSettings
        {
            Profiles = new List<ISynthesisProfileSettings>
            {
                new SynthesisProfile
                {
                    ID = profileId,
                    Nickname = profileNickname ?? "Test Profile",
                    TargetRelease = gameRelease ?? GameRelease.SkyrimSE,
                    DataPathOverride = DataFolder,
                    MutagenVersioning = NugetVersioningEnum.Manual,
                    MutagenManualVersion = mutagenVersion,
                    SynthesisVersioning = NugetVersioningEnum.Manual,
                    SynthesisManualVersion = synthesisVersion,
                    SplitIfMaxMastersExceeded = splitIfMaxMastersExceeded,
                    Groups = new List<PatcherGroupSettings>
                    {
                        new PatcherGroupSettings
                        {
                            On = true,
                            Name = groupName,
                            Patchers = patchers.ToList()
                        }
                    }
                }
            }
        };

        var guiSettings = new SynthesisGuiSettings
        {
            SelectedProfile = profileId
        };

        // Write settings files
        var pipelineSettingsPath = Path.Combine(TestFolder, "PipelineSettings.json");
        var guiSettingsPath = Path.Combine(TestFolder, "GuiSettings.json");
        File.WriteAllText(pipelineSettingsPath,
            JsonConvert.SerializeObject(pipelineSettings, Formatting.Indented, Execution.Constants.JsonSettings));
        File.WriteAllText(guiSettingsPath,
            JsonConvert.SerializeObject(guiSettings, Formatting.Indented, Execution.Constants.JsonSettings));

        return profileId;
    }

    public TPayload GetComponentPayload<TPayload, TUserPayload>(Action<ContainerBuilder>? configureContainer = null)
        where TPayload : notnull
        where TUserPayload : notnull
    {
        // Setup DI container with overrides
        var builder = new ContainerBuilder();
        builder.RegisterModule(new IntegrationTestModule(this, Mode));
        builder.RegisterType<TUserPayload>().AsSelf();
        builder.RegisterType<TPayload>().AsSelf();

        // Allow test to customize container registrations
        configureContainer?.Invoke(builder);

        var container = builder.Build();

        _disposable.Add(container);
        return container.Resolve<TPayload>();
    }

    /// <summary>
    /// Interface for internal initialization payload
    /// </summary>
    protected interface IInitializationPayload
    {
        IStartup Startup { get; }
        IStartupTracker StartupTracker { get; }
        ProfileManagerVm ProfileManager { get; }
        ISchedulerProvider SchedulerProvider { get; }
        ActiveRunVm ActiveRunVm { get; }
    }

    /// <summary>
    /// Internal wrapper that combines user payload with required initialization components
    /// </summary>
    protected record InitializationPayload<TUserPayload>(
        IStartup Startup,
        IStartupTracker StartupTracker,
        ProfileManagerVm ProfileManager,
        ISchedulerProvider SchedulerProvider,
        ActiveRunVm ActiveRunVm,
        TUserPayload UserPayload) : IInitializationPayload
        where TUserPayload : notnull;

    /// <summary>
    /// Gets component payload and performs full initialization including:
    /// - Verifying StartupTracker starts uninitialized
    /// - Initializing the application
    /// - Waiting for profile to be selected
    /// - Waiting for profile state to succeed
    /// - Waiting for patchers to be ready to run
    /// </summary>
    /// <returns>The user payload after initialization is complete</returns>
    public async Task<TUserPayload> GetComponentPayloadAndInitialize<TUserPayload>(Action<ContainerBuilder>? configureContainer = null)
        where TUserPayload : notnull
    {
        // Get the internal payload with both user components and initialization components
        var payload = GetComponentPayload<InitializationPayload<TUserPayload>, TUserPayload>(configureContainer);

        // Store the internal payload for reuse in helper methods
        _internalPayload = payload;

        // Verify StartupTracker is not initialized before startup
        payload.StartupTracker.Initialized.ShouldBeFalse("StartupTracker should not be initialized before Startup.Initialize()");

        // Wait for StartupTracker to be initialized
        Output.WriteLine("Waiting for startup to complete...");
        var startupTask = payload.Startup.Initialize();

        var initializedResult = await payload.StartupTracker.WhenAnyValue(x => x.Initialized)
            .Where(initialized => initialized)
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(60));

        initializedResult.ShouldBeTrue("StartupTracker should be initialized after Startup.Initialize()");
        Output.WriteLine("Startup completed and tracker initialized");

        // Ensure the initialization task completes
        await startupTask;

        // Let the throttles and debounces shake out
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Wait for profile to be selected and ready
        Output.WriteLine("Waiting for profile to be selected...");
        var selectedProfile = await payload.ProfileManager.WhenAnyValue(x => x.SelectedProfile)
            .Where(p => p != null)
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(30));

        selectedProfile.ShouldNotBeNull("SelectedProfile should be set after initialization");

        // Flush the dispatcher queue to ensure all queued operations complete
        // This is important because some VM setups enqueue on the GUI main thread,
        // and their failures can be queued on the dispatcher while the test sees success
        Output.WriteLine("Flushing dispatcher queue...");
        await Observable.Return(System.Reactive.Unit.Default)
            .ObserveOn(payload.SchedulerProvider.MainThread)
            .FirstAsync();

        // Wait for profile state to be successful (load order loaded, etc)
        Output.WriteLine("Waiting for profile state to be successful...");
        var profileState = await selectedProfile.WhenAnyValue(x => x.State)
            .Where(state => state.Succeeded)
            .FirstAsync()
            .Timeout(TimeSpan.FromSeconds(30));

        profileState.Succeeded.ShouldBeTrue($"Profile state should be successful. Error: {profileState.Reason}");

        // Check for any patcher errors after dispatcher flush
        Output.WriteLine("Checking patcher states...");
        foreach (var group in selectedProfile.Groups.Items)
        {
            foreach (var patcher in group.Patchers.Items)
            {
                patcher.State.RunnableState.Succeeded.ShouldBeTrue(
                    $"Patcher '{patcher.NameVm.Name}' should be in successful state. Error: {patcher.State.RunnableState.Reason}");
            }
        }

        // Wait for patcher to be ready (built and prepared)
        Output.WriteLine("Waiting for patcher to be ready...");
        var readyToRun = await payload.ProfileManager.RunPatchers.CanExecute
            .FirstAsync(canExecute => canExecute)
            .Timeout(TimeSpan.FromMinutes(2)); // Give it time to build

        readyToRun.ShouldBeTrue("RunPatchers command should be executable after build completes");
        
        Output.WriteLine("Initialization complete - ready to run patchers");

        return payload.UserPayload;
    }

    /// <summary>
    /// Gets the stored internal payload
    /// </summary>
    protected IInitializationPayload GetStoredPayload()
    {
        if (_internalPayload == null)
        {
            throw new InvalidOperationException(
                "Internal payload has not been initialized. " +
                "Did you call GetComponentPayloadAndInitialize first?");
        }
        return _internalPayload;
    }

    /// <summary>
    /// Gets the stored internal payload with user payload access
    /// </summary>
    private InitializationPayload<TUserPayload> GetStoredPayloadWithUser<TUserPayload>()
        where TUserPayload : notnull
    {
        if (_internalPayload is not InitializationPayload<TUserPayload> payload)
        {
            throw new InvalidOperationException(
                $"Internal payload is not of expected type InitializationPayload<{typeof(TUserPayload).Name}>. " +
                "Did you call GetComponentPayloadAndInitialize first?");
        }
        return payload;
    }

    public record EmptyPayload
    {
    }

    /// <summary>
    /// Executes the patcher pipeline and waits for completion
    /// </summary>
    public async Task RunPatcherPipeline()
    {
        await GetComponentPayloadAndInitialize<EmptyPayload>();
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
            .Timeout(TimeSpan.FromMinutes(2));

        Output.WriteLine("Run completed");
    }

    /// <summary>
    /// Ensures the active run has no errors
    /// </summary>
    public void EnsureActiveRunHasNoErrors()
    {
        var payload = GetStoredPayload();
        payload.ActiveRunVm.CurrentRun!.ResultError.ShouldBeNull(
            $"Run should complete without errors. Error: {payload.ActiveRunVm.CurrentRun.ResultError}");
    }

    /// <summary>
    /// Executes the patcher pipeline with a ThrowingBuild to verify build meta caching is working.
    /// This should only be called after a successful run that created the build meta.
    /// </summary>
    public async Task RunPatcherPipelineWithThrowingBuild()
    {
        await GetComponentPayloadAndInitialize<EmptyPayload>(builder =>
        {
            // Override IBuild with ThrowingBuild to verify it's not called
            builder.RegisterInstance(new TestUtilities.ThrowingBuild())
                .As<IBuild>()
                .SingleInstance();
        });

        var payload = GetStoredPayload();

        // Execute RunPatchers command on the UI thread
        Output.WriteLine("Executing RunPatchers command with ThrowingBuild...");
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
            .Timeout(TimeSpan.FromMinutes(2));

        Output.WriteLine("Run completed without calling IBuild.Compile (build meta caching worked!)");
    }
}
