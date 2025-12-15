using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.CLI.Services.Git;

public class PrepareGitPatcherForCli
{
    private readonly IGitPatcherPrep _gitPatcherPrep;
    private readonly UpdateGitRunnerToSettings _updateGitRunnerToSettings;
    private readonly IBuildMetaFileReader _buildMetaFileReader;
    private readonly ShouldShortCircuitCompilation _shouldShortCircuitCompilation;
    private readonly IGitPatcherCompilation _gitPatcherCompilation;
    private readonly ILogger _logger;
    private readonly IQueryInstalledSdk _queryInstalledSdk;

    public PrepareGitPatcherForCli(
        IGitPatcherPrep gitPatcherPrep,
        UpdateGitRunnerToSettings updateGitRunnerToSettings,
        IBuildMetaFileReader buildMetaFileReader,
        ShouldShortCircuitCompilation shouldShortCircuitCompilation,
        IGitPatcherCompilation gitPatcherCompilation,
        ILogger logger,
        IQueryInstalledSdk queryInstalledSdk)
    {
        _gitPatcherPrep = gitPatcherPrep;
        _updateGitRunnerToSettings = updateGitRunnerToSettings;
        _buildMetaFileReader = buildMetaFileReader;
        _shouldShortCircuitCompilation = shouldShortCircuitCompilation;
        _gitPatcherCompilation = gitPatcherCompilation;
        _logger = logger;
        _queryInstalledSdk = queryInstalledSdk;
    }

    public async Task Prepare(CancellationToken cancel)
    {
        // Clone/prep the git repository
        await _gitPatcherPrep.Prep(cancel);

        // Checkout and get runner info
        var repoInfo = await _updateGitRunnerToSettings.Sync(cancel);

        // Check if we can short circuit compilation
        var meta = _buildMetaFileReader.Read(repoInfo.MetaPath);
        if (_shouldShortCircuitCompilation.ShouldShortCircuit(repoInfo, meta))
        {
            _logger.Information("Short circuiting compilation - build metadata indicates project is already built");
            return;
        }

        // Query the installed .NET SDK version
        var dotNetVersion = await _queryInstalledSdk.Query(cancel);

        // Compile the project
        _logger.Information("Compiling Git patcher project");
        var compileResult = await _gitPatcherCompilation.Compile(repoInfo, dotNetVersion, cancel);
        if (compileResult.Failed)
        {
            throw new InvalidOperationException($"Failed to compile Git patcher: {compileResult.Reason}");
        }

        _logger.Information("Git patcher compilation completed successfully");
    }
}