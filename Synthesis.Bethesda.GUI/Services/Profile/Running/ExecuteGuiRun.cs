using System.IO;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Strings;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile.Services;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services.Profile.Running;

public interface IExecuteGuiRun
{
    Task Run(
        IEnumerable<IGroupRun> groupRuns,
        PersistenceMode persistenceMode,
        bool localize,
        bool utf8InEmbeddedStrings,
        float? headerVersionOverride,
        FormIDRangeMode formIDRangeMode,
        Language targetLanguage,
        bool masterFile,
        bool masterStyleFallbackEnabled,
        MasterStyle masterStyle,
        CancellationToken cancel);
}

public class ExecuteGuiRun : IExecuteGuiRun
{
    private readonly IExecuteRun _executeRun;
    private readonly IDataDirectoryProvider _dataDirectoryProvider;
    private readonly IProfileDirectories _profileDirectories;

    public ExecuteGuiRun(
        IExecuteRun executeRun,
        IDataDirectoryProvider dataDirectoryProvider,
        IProfileDirectories profileDirectories)
    {
        _executeRun = executeRun;
        _dataDirectoryProvider = dataDirectoryProvider;
        _profileDirectories = profileDirectories;
    }
        
    public async Task Run(
        IEnumerable<IGroupRun> groupRuns,
        PersistenceMode persistenceMode,
        bool localize,
        bool utf8InEmbeddedStrings,
        float? headerVersionOverride,
        FormIDRangeMode formIDRangeMode,
        Language targetLanguage,
        bool masterFile,
        bool masterStyleFallbackEnabled,
        MasterStyle masterStyle,
        CancellationToken cancel)
    {
        var outputDir = _dataDirectoryProvider.Path;
        await _executeRun.Run(
            groups: groupRuns.ToArray(),
            cancel: cancel,
            outputDir: outputDir,
            runParameters: new RunParameters(
                TargetLanguage: targetLanguage,
                Localize: localize,
                UseUtf8ForEmbeddedStrings: utf8InEmbeddedStrings,
                FormIDRangeMode: formIDRangeMode,
                HeaderVersionOverride: headerVersionOverride,
                PersistenceMode: persistenceMode,
                PersistencePath: Path.Combine(_profileDirectories.ProfileDirectory, "Persistence"),
                Master: masterFile,
                MasterStyleFallbackEnabled: masterStyleFallbackEnabled,
                MasterStyle: masterStyle)).ConfigureAwait(false);
    }
}