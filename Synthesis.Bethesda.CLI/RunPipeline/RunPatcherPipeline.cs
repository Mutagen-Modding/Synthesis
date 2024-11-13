using System.IO.Abstractions;
using Autofac;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V2;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPatcherPipeline
{
    private readonly ILifetimeScope _scope;
    private readonly IFileSystem _fileSystem;
    private readonly ProfileRetriever _profileRetriever;
    private readonly IPipelineSettingsV2Reader _pipelineSettingsV2Reader;
    public RunPatcherPipelineCommand Command { get; }

    public RunPatcherPipeline(
        ILifetimeScope scope,
        IFileSystem fileSystem,
        ProfileRetriever profileRetriever,
        IPipelineSettingsV2Reader pipelineSettingsV2Reader,
        RunPatcherPipelineCommand command)
    {
        _scope = scope;
        _fileSystem = fileSystem;
        _profileRetriever = profileRetriever;
        _pipelineSettingsV2Reader = pipelineSettingsV2Reader;
        Command = command;
    }
        
    public async Task Run(CancellationToken cancel)
    {
        var pipelineSettingsPath = Command.PipelineSettingsPath;

        if (!_fileSystem.File.Exists(pipelineSettingsPath))
        {
            throw new FileNotFoundException("Could not find settings", pipelineSettingsPath);
        }
        var pipeSettings = _pipelineSettingsV2Reader.Read(pipelineSettingsPath);

        var profile = _profileRetriever.GetProfile(pipeSettings.Profiles, pipelineSettingsPath, Command.ProfileIdentifier);

        using var profileScope = _scope.BeginLifetimeScope(LifetimeScopes.ProfileNickname, (b) =>
        {
            b.RegisterInstance(pipeSettings).AsImplementedInterfaces();
            b.RegisterInstance(profile).AsImplementedInterfaces();
        });

        var getGroupRunners = profileScope.Resolve<IGetGroupRunners>();

        using var runScope = profileScope.BeginLifetimeScope(LifetimeScopes.RunNickname, (b) =>
        {
        });
        var executeRun = runScope.Resolve<IExecuteRun>();
        var printDotNet = runScope.Resolve<PrintDotNetInfo>();
        await printDotNet.Print(CancellationToken.None);
        
        await executeRun
            .Run(
                groups: getGroupRunners.Get(cancel),
                outputDir: Command.OutputDirectory,
                cancel: cancel,
                runParameters: new RunParameters(
                    TargetLanguage: profile.TargetLanguage,
                    Localize: profile.Localize,
                    UseUtf8ForEmbeddedStrings: profile.UseUtf8ForEmbeddedStrings,
                    HeaderVersionOverride: profile.HeaderVersionOverride,
                    FormIDRangeMode: profile.FormIDRangeMode,
                    PersistenceMode: Command.PersistenceMode ?? PersistenceMode.None, 
                    PersistencePath: Command.PersistencePath,
                    Master: profile.ExportAsMasterFiles,
                    MasterStyleFallbackEnabled: profile.MasterStyleFallbackEnabled,
                    MasterStyle: profile.MasterStyle)).ConfigureAwait(false);
    }
}