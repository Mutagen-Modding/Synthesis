# Development Notes

This file contains helpful information for developers and AI assistants working on this codebase.

## About Synthesis

Synthesis is a framework and GUI for creating Bethesda game mods via code instead of by hand. It allows users to run code-based "patchers" that read game data and output plugin files. The framework handles Git repository management, compilation, execution orchestration, and provides both GUI (WPF) and CLI interfaces.

## Project Structure

### Solutions

The repository contains multiple solution files:

- **Synthesis.Bethesda.sln** - Main solution (use this for development)
- **Synthesis.Bethesda.SettingsHost.sln** - Settings-specific solution

### Key Projects

**Core Libraries:**
- **Synthesis.Bethesda** - Core abstractions, DTOs, and command definitions. Minimal dependencies.
- **Mutagen.Bethesda.Synthesis** - NuGet package that patcher developers reference. Provides `SynthesisPipeline` API and `IPatcher` interface for patcher discovery.
- **Synthesis.Bethesda.Execution** - Orchestrates patcher discovery, Git operations, compilation, and execution. Heavy dependencies (Autofac, LibGit2Sharp, MSBuild, Roslyn).

**UI Projects:**
- **Synthesis.Bethesda.GUI** - Full WPF application with Autofac DI and ReactiveUI MVVM.
- **Synthesis.Bethesda.CLI** - Command-line interface for running pipelines without GUI.
- **Mutagen.Bethesda.Synthesis.WPF** - Reusable WPF components and ViewModels.

**Testing:**
- **Synthesis.Bethesda.UnitTests** - Unit test project
- **Synthesis.Bethesda.IntegrationTests** - Integration test project

## Building

### Prerequisites

- .NET 9.0 SDK
- Windows (GUI is WPF-based and Windows-only)

### Build Commands

```bash
# Restore dependencies
dotnet restore Synthesis.Bethesda.sln

# Build all projects (Release configuration recommended for testing)
dotnet build Synthesis.Bethesda.sln -c Release

# Build specific project
dotnet build Synthesis.Bethesda.GUI/Synthesis.Bethesda.GUI.csproj -c Release
```

### Cleaning the Repository

If you encounter build errors, clean the repository:

```bash
dotnet clean Synthesis.Bethesda.sln
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test Synthesis.Bethesda.sln -c Release

# Run tests for specific project
dotnet test Synthesis.Bethesda.UnitTests/Synthesis.Bethesda.UnitTests.csproj -c Release
```

## Running the Applications

```bash
# Run GUI (from project directory)
dotnet run --project Synthesis.Bethesda.GUI/Synthesis.Bethesda.GUI.csproj

# Run CLI
dotnet run --project Synthesis.Bethesda.CLI/Synthesis.Bethesda.CLI.csproj -- <command>
```

## Architecture

### Package Management

- This solution uses **Central Package Management** (Directory.Packages.props)
- All package versions are defined centrally in Directory.Packages.props
- Target framework: .NET 9.0 (defined in Directory.Build.props)
- Platform: x64 only

### Key Architectural Patterns

**Dependency Injection (Autofac):**
- Main DI modules: `Synthesis.Bethesda.Execution.Modules.MainModule` and `Synthesis.Bethesda.GUI.Modules.MainModule`
- Lifetime scopes: `ProfileNickname` and `RunNickname` for hierarchical scoping
- Convention-based registration via `.TypicalRegistrations()`

**Reactive Programming:**
- GUI uses ReactiveUI extensively for ViewModels
- System.Reactive for observables and async state management
- Reporter pattern uses Rx for progress tracking

**Settings & Configuration:**
- JSON-based profile system (`SynthesisProfile`) with versioning
- Profiles contain groups of patchers, each group produces one output plugin
- Settings are automatically upgraded via `PipelineSettingsUpgrader`

### Patcher Types

1. **Git Patcher** (`GitPatcherRun`):
   - Clones a Git repository, builds, and runs the code
   - Handles branch/tag/commit targeting and NuGet version management
   - Delegates to SolutionPatcherRun after cloning

2. **Solution Patcher** (`SolutionPatcherRun`):
   - Points to a local .csproj file, builds and runs it
   - Used internally by Git patchers

3. **CLI Patcher** (`CliPatcherRun`):
   - Runs any external executable
   - Most flexible but least integrated

### Execution Model

**Pipeline Flow:**
```
ExecuteRun.Run()
  → RunAllGroups.Run()        # Process each group
    → RunAGroup.Run()         # Single group execution
      → GroupRunPreparer.Prepare()    # Create base patch
      → RunSomePatchers.Run()         # Run patchers in sequence
        → RunAPatcher.Run()           # Single patcher
          → prepBundle.Prep()         # Build/compile
          → prepBundle.Run.Run()      # Execute
          → FinalizePatcherRun.Finalize()
      → PostRunProcessor.Run()        # Post-processing
      → MoveFinalResults.Move()       # Copy to output
```

**Key Classes:**
- `ExecuteRun` (Synthesis.Bethesda.Execution/Running/Runner/ExecuteRun.cs): Top-level orchestrator
- `RunAGroup` (Running/Runner/RunAGroup.cs): Manages a single group (produces one output plugin)
- `RunAPatcher` (Running/Runner/RunAPatcher.cs): Executes a single patcher
- `GitPatcherRun` (Patchers/Running/Git/GitPatcherRun.cs): Git clone/update and delegation to SolutionPatcherRun
- `SolutionPatcherRun` (Patchers/Running/Solution/SolutionPatcherRun.cs): Compiles and executes .csproj patchers

### Git Patcher Workflow

1. **Prep Phase**: Clone/update repository, checkout target, delegate to SolutionPatcherRun
2. **Build Phase**: Compile .csproj using `dotnet build`, handle NuGet versioning
3. **Run Phase**: Execute with `dotnet run`, passing structured arguments (`RunSynthesisPatcher`)

### Patcher Developer API

Patchers reference `Mutagen.Bethesda.Synthesis` NuGet and use `SynthesisPipeline`:

```csharp
await SynthesisPipeline.Instance
    .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
    .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
    .Run(args);

static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
{
    // state.LoadOrder for reading
    // state.PatchMod for writing
}
```

The pipeline handles CLI parsing, load order construction, settings GUI integration, and plugin export.

### Logging Guidelines: Runner vs Patcher

Synthesis has two distinct "sides" with different logging approaches:

**Runner Side** (Synthesis.Bethesda.Execution, Synthesis.Bethesda.GUI, Synthesis.Bethesda.CLI):
- Code that orchestrates patcher execution, manages Git repos, handles compilation, etc.
- **Use `ILogger`** for all logging
- Inject via constructor: `private readonly ILogger _logger;`
- Use appropriate log levels:
  - `_logger.Information()` - General informational messages
  - `_logger.Debug()` - Detailed debugging information
  - `_logger.Warning()` - Warnings that don't prevent execution
  - `_logger.Error()` - Errors that prevent operation
- Examples: ExecuteRun, RunAGroup, GitPatcherRun, BuildPatcher

**Patcher Side** (Mutagen.Bethesda.Synthesis, Synthesis.Bethesda):
- Code that patchers directly use or that runs within the patcher process
- **Use `Console.WriteLine()`** for logging
- This output goes to the patcher's console and is captured by the runner
- Examples: PatcherStateFactory, SynthesisPipeline, IPatcherState implementations

**Why the distinction?**
- The runner has access to full DI infrastructure and uses ILogger to route logs to GUI, files, or console
- Patchers run as separate processes and their console output is captured and displayed by the runner
- Mutagen.Bethesda.Synthesis is distributed as a NuGet package and cannot depend on Synthesis logging infrastructure

### Build System

- Uses `dotnet build` via process execution (Synthesis.Bethesda.Execution/DotNet/Builder/Build.cs)
- Builds are queued through work engine to prevent parallel build conflicts
- MSBuild errors parsed via Roslyn

### Versioning Strategy

Patchers can specify Mutagen/Synthesis library versions:
- **Profile**: Use version from profile settings
- **Match**: Match what the Git repo specifies
- **Latest**: Always use latest available
- **Manual**: User-specified version

## Development Workflow

### Always Verify Your Changes

**Important**: After making code changes, always verify the solution builds and tests pass:

```bash
# 1. Clean the solution (if needed)
dotnet clean Synthesis.Bethesda.sln

# 2. Build the solution
dotnet build Synthesis.Bethesda.sln

# 3. Run tests to verify nothing broke
dotnet test Synthesis.Bethesda.sln
```

This ensures your changes don't break the build or existing functionality.

### File System Operations

- **NEVER redirect to `nul`** - On Windows, `2>nul` creates unwanted files that Git tracks
- Use proper null redirection: `2>/dev/null` (works on Windows with bash)
- For temporary files, use `.claude/` subfolder or designated temp directories that are gitignored
- Example: `ls directory 2>/dev/null || echo "Not found"` instead of `dir directory 2>nul`
- **NEVER use `sed` for bulk find/replace** - `sed` does not preserve Windows CRLF line endings, creating massive spurious diffs
  - On Windows, `sed -i` converts CRLF to LF, causing every line to show as changed in git
  - Use targeted edits with the Edit tool instead of global sed replacements
  - If you must do bulk replacements, only use tools that preserve line endings (e.g., PowerShell with `-Raw` and explicit encoding)
  - Example (incorrect): `find . -name "*.cs" -exec sed -i 's/OldName/NewName/g' {} \;` - creates CRLF→LF changes on every touched file
  - Example (correct): Use Edit tool on each file individually, or ask us

### Documentation Guidelines

When writing Markdown documentation in the `docs/` folder:

**List Formatting:**
- **Always include a blank line before lists** (both bulleted and numbered)
- Without the blank line, MkDocs will not render the list properly
- This applies to lists after headers, paragraphs, or any other content

**Example - Incorrect:**
```markdown
## Features
- Feature 1
- Feature 2
```

**Example - Correct:**
```markdown
## Features

- Feature 1
- Feature 2
```

**Why this matters:**
- MkDocs uses Python-Markdown which requires blank lines before lists for proper parsing
- Without blank lines, lists appear as plain text instead of formatted bullets/numbers
- This is a common source of formatting issues in the documentation

## Releases

### Release Versioning

- Create release tags using semantic versioning format: `<major>.<minor>.<patch>`
- Always include the patch number, even if it's zero (e.g., `3.1.0`, not `3.1`)
- **Do not prefix with `v`** (e.g., use `3.1.0`, not `v3.1.0`)
- This format is required for GitVersion compatibility

### Creating GitHub Release Drafts

1. Find the last release tag: `git tag --sort=-version:refname`
2. Get commits since last release: `git log --oneline <last-tag>..HEAD`
3. Construct release notes by categorizing commits:
   - **Enhancements**: New features, performance improvements, major changes
   - **Bug Fixes**: Bug fixes and corrections
   - **Testing & Documentation**: Test additions, documentation updates
4. Create draft release: `gh release create <version> --draft --title "<version>" --notes "<release-notes>" --target main`
5. Include full changelog link: `**Full Changelog**: https://github.com/Noggog/Synthesis/compare/<last-tag>...<new-tag>`

## Important Notes

- **Platform**: x64 only (defined in Directory.Build.props)
- **Nullable**: Enabled project-wide with warnings as errors
- **Documentation**: XML documentation files are generated for all projects
- **Git Operations**: LibGit2Sharp is used for all Git operations
- **Compilation**: Uses Microsoft.Build and Roslyn for project compilation and analysis
- **WPF**: GUI is Windows-only (uses WPF and targets Windows platform)

## Contributing

See the main README.md and official documentation for contribution guidelines.
