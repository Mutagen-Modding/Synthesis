using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.CLI.Services.Git;
using Synthesis.Bethesda.CLI.Services.Solution;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class PrepForRun
{
    private readonly ILifetimeScope _scope;
    private readonly ProfileProvider _profileProvider;

    public PrepForRun(
        ILifetimeScope scope,
        ProfileProvider profileProvider)
    {
        _scope = scope;
        _profileProvider = profileProvider;
    }

    public async Task Prep(CancellationToken cancel)
    {
        foreach (var patcher in _profileProvider.Profile.Value.Groups.SelectMany(x => x.Patchers))
        {
            switch (patcher)
            {
                case CliPatcherSettings:
                    break;
                case GithubPatcherSettings githubPatcherSettings:
                    var gitScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
                    {
                        c.RegisterModule<GitPatcherModule>();
                        
                        c.RegisterAssemblyTypes(typeof(PrepareGitPatcher).Assembly)
                            .InNamespacesOf(
                                typeof(PrepareGitPatcher))
                            .AsSelf()
                            .AsImplementedInterfaces()
                            .SingleInstance();
                        
                        c.RegisterInstance(githubPatcherSettings)
                            .AsSelf()
                            .AsImplementedInterfaces()
                            .SingleInstance();
                    });
                    await gitScope.Resolve<PrepareGitPatcher>().Prepare(cancel);
                    break;
                case SolutionPatcherSettings solutionPatcherSettings:
                    var slnScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
                    {
                        c.RegisterModule<SolutionPatcherModule>();
                        
                        c.RegisterAssemblyTypes(typeof(PrepareSolutionPatcher).Assembly)
                            .InNamespacesOf(
                                typeof(PrepareSolutionPatcher))
                            .AsSelf()
                            .AsImplementedInterfaces()
                            .SingleInstance();
                        
                        c.RegisterInstance(solutionPatcherSettings)
                            .AsSelf()
                            .AsImplementedInterfaces();
                    });
                    await slnScope.Resolve<PrepareSolutionPatcher>().Prepare(cancel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(patcher));
            }
        }
    }
}