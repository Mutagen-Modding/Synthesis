using Autofac;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPatcherPipeline
{
    private readonly ILifetimeScope _scope;
    private readonly ProfileProvider _profileProvider;
    private readonly RunPatcherPipelineCommand _command;

    public RunPatcherPipeline(
        ILifetimeScope scope,
        ProfileProvider profileProvider,
        RunPatcherPipelineCommand command)
    {
        _scope = scope;
        _profileProvider = profileProvider;
        _command = command;
    }
        
    public async Task Run(CancellationToken cancel)
    {
        var profile = _profileProvider.Profile.Value;
        using var profileScope = _scope.BeginLifetimeScope(LifetimeScopes.ProfileNickname, (b) =>
        {
        });
        
        var printDotNet = profileScope.Resolve<PrintDotNetInfo>();
        await printDotNet.Print(cancel);

        var prep = profileScope.Resolve<PrepForRun>();
        await prep.Prep(cancel);

        using var runScope = profileScope.BeginLifetimeScope(LifetimeScopes.RunNickname, (b) =>
        {
        });
        var executeRun = runScope.Resolve<IExecuteRun>();
        
        var getGroupRunners = profileScope.Resolve<IGetGroupRunners>();
        var groupRuns = getGroupRunners.Get(cancel);
        await executeRun
            .Run(
                groups: groupRuns,
                outputDir: _command.OutputDirectory,
                cancel: cancel,
                runParameters: new RunParameters(
                    TargetLanguage: profile.TargetLanguage,
                    Localize: profile.Localize,
                    UseUtf8ForEmbeddedStrings: profile.UseUtf8ForEmbeddedStrings,
                    HeaderVersionOverride: profile.HeaderVersionOverride,
                    FormIDRangeMode: profile.FormIDRangeMode,
                    PersistenceMode: _command.PersistenceMode ?? PersistenceMode.None, 
                    PersistencePath: _command.PersistencePath,
                    Master: profile.ExportAsMasterFiles,
                    MasterStyleFallbackEnabled: profile.MasterStyleFallbackEnabled,
                    MasterStyle: profile.MasterStyle)).ConfigureAwait(false);
    }
}