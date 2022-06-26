using Autofac;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public interface IPatcherSettingsToRunnerFactory
{
    IPatcherRun Convert(PatcherSettings patcherSettings);
}

public class PatcherSettingsToRunnerFactory : IPatcherSettingsToRunnerFactory
{
    private readonly ILifetimeScope _scope;

    public PatcherSettingsToRunnerFactory(ILifetimeScope scope)
    {
        _scope = scope;
    }
    
    public IPatcherRun Convert(PatcherSettings patcherSettings)
    {
        switch (patcherSettings)
        {
            case CliPatcherSettings cliPatcherSettings:
            {
                var scope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
                {
                    c.RegisterModule<PatcherModule>();
                    c.RegisterInstance(cliPatcherSettings)
                        .AsSelf()
                        .AsImplementedInterfaces();
                });
                return scope.Resolve<ICliPatcherRun>();
            }
            case GithubPatcherSettings githubPatcherSettings:
            {
                var scope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
                {
                    c.RegisterModule<GitPatcherModule>();
                    c.RegisterInstance(githubPatcherSettings)
                        .AsSelf()
                        .AsImplementedInterfaces();
                });
                scope = scope.BeginLifetimeScope(LifetimeScopes.RunNickname);
                return scope.Resolve<IGitPatcherRun>();
            }
            case SolutionPatcherSettings solutionPatcherSettings:
            {
                var scope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
                {
                    c.RegisterModule<SolutionPatcherModule>();
                    c.RegisterInstance(solutionPatcherSettings)
                        .AsSelf()
                        .AsImplementedInterfaces();
                });
                scope = scope.BeginLifetimeScope(LifetimeScopes.RunNickname);
                return scope.Resolve<ISolutionPatcherRun>();
            }
            default:
                throw new NotImplementedException();
        }
    }
}