using Noggog;
using Serilog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Settings.Json;
using Synthesis.Bethesda.Execution.Utility;
using Noggog.WorkEngine;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IGetSettingsStyle
{
    Task<SettingsConfiguration> Get(
        string executablePath,
        FilePath? buildMetaPath,
        CancellationToken cancel);

    Task<SettingsConfiguration> CompileAndGetForProject(
        string projectPath,
        CancellationToken cancel);
}

public class GetSettingsStyle : IGetSettingsStyle
{
    private readonly ILogger _logger;
    private readonly IWorkDropoff _workDropoff;
    private readonly IBuildMetaFileReader _metaFileReader;
    private readonly IBuild _build;
    private readonly IBuildLock _buildLock;
    private readonly IQueryExecutablePath _queryExecutablePath;
    private readonly IShortCircuitSettingsProvider _shortCircuitSettingsProvider;
    private readonly IWriteShortCircuitMeta _writeShortCircuitMeta;
    public ILinesToReflectionConfigsParser LinesToConfigsParser { get; }
    public ISynthesisSubProcessRunner ProcessRunner { get; }
    public IRunProcessStartInfoProvider GetRunProcessStartInfoProvider { get; }

    public GetSettingsStyle(
        ILogger logger,
        ISynthesisSubProcessRunner processRunner,
        IWorkDropoff workDropoff,
        IBuildMetaFileReader metaFileReader,
        IBuild build,
        IBuildLock buildLock,
        IQueryExecutablePath queryExecutablePath,
        IShortCircuitSettingsProvider shortCircuitSettingsProvider,
        ILinesToReflectionConfigsParser linesToConfigsParser,
        IWriteShortCircuitMeta writeShortCircuitMeta,
        IRunProcessStartInfoProvider getRunProcessStartInfoProvider)
    {
        _logger = logger;
        _workDropoff = workDropoff;
        _metaFileReader = metaFileReader;
        _build = build;
        _buildLock = buildLock;
        _queryExecutablePath = queryExecutablePath;
        _shortCircuitSettingsProvider = shortCircuitSettingsProvider;
        _writeShortCircuitMeta = writeShortCircuitMeta;
        LinesToConfigsParser = linesToConfigsParser;
        ProcessRunner = processRunner;
        GetRunProcessStartInfoProvider = getRunProcessStartInfoProvider;
    }
        
    public async Task<SettingsConfiguration> Get(
        string executablePath,
        FilePath? buildMetaPath,
        CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();

        var meta = buildMetaPath != null ? _metaFileReader.Read(buildMetaPath.Value) : default;

        if (_shortCircuitSettingsProvider.Shortcircuit && meta?.SettingsConfiguration != null)
        {
            _logger.Information("Getting settings style from meta path: {Path}", buildMetaPath);
            return meta.SettingsConfiguration;
        }

        var settingsConfig = await ExecuteSettingsRetrievalFromExecutable(executablePath, cancel);

        cancel.ThrowIfCancellationRequested();

        if (meta != null
            && buildMetaPath != null)
        {
            meta = meta with
            {
                SettingsConfiguration = settingsConfig
            };
            _writeShortCircuitMeta.WriteMeta(buildMetaPath.Value, meta);
        }

        return settingsConfig;
    }

    public async Task<SettingsConfiguration> CompileAndGetForProject(
        string projectPath,
        CancellationToken cancel)
    {
        return await _workDropoff.EnqueueAndWait(async () =>
        {
            var buildResult = await _build.Compile(projectPath, cancel);
            if (buildResult.Failed)
            {
                _logger.Error("Could not build solution patcher in order to query for settings: {Error}", buildResult);
                return new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>());
            }

            using (await _buildLock.GetLock(projectPath).WaitAsync())
            {
                return await ExecuteSettingsRetrievalFromProject(projectPath, cancel);
            }
        }, cancel).ConfigureAwait(false);
    }

    private async Task<SettingsConfiguration> ExecuteSettingsRetrievalFromExecutable(string executablePath, CancellationToken cancel)
    {
        return await _workDropoff.EnqueueAndWait(async () =>
        {
            var start = GetRunProcessStartInfoProvider.GetStart(executablePath, new SettingsQuery());

            var result = await ProcessRunner.RunAndCapture(
                start,
                cancel: cancel);

            return ParseSettingsResult(result);
        }, cancel).ConfigureAwait(false);
    }

    private async Task<SettingsConfiguration> ExecuteSettingsRetrievalFromProject(string projectPath, CancellationToken cancel)
    {
        var executablePath = await _queryExecutablePath.Query(projectPath, cancel).ConfigureAwait(false);
        if (executablePath.Failed)
        {
            _logger.Error("Could not locate built solution patcher executable to query for settings: {Error}", executablePath.Reason);
            return new SettingsConfiguration(SettingsStyle.None, Array.Empty<ReflectionSettingsConfig>());
        }

        var start = GetRunProcessStartInfoProvider.GetStart(executablePath.Value, new SettingsQuery());

        var result = await ProcessRunner.RunAndCapture(
            start,
            cancel: cancel);

        return ParseSettingsResult(result);
    }

    private SettingsConfiguration ParseSettingsResult(ProcessRunReturn result)
    {
        switch ((Codes)result.Result)
        {
            case Codes.OpensForSettings:
                return new SettingsConfiguration(SettingsStyle.Open, []);
            case Codes.AutogeneratedSettingsClass:
                return new SettingsConfiguration(
                    SettingsStyle.SpecifiedClass,
                    LinesToConfigsParser.Parse(result.Out).Configs);
            default:
                return new SettingsConfiguration(SettingsStyle.None, []);
        }
    }
}