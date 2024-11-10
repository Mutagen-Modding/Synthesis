using Autofac;
using Noggog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

public interface IPatcherFactory
{
    PatcherVm Get(PatcherSettings settings);
    GitPatcherVm GetGitPatcher(GithubPatcherSettings? settings = null);
    SolutionPatcherVm GetSolutionPatcher(SolutionPatcherSettings settings);
    CliPatcherVm GetCliPatcher(CliPatcherSettings settings);
}
    
public class PatcherFactory : IPatcherFactory
{
    private readonly ILifetimeScope _scope;
    private readonly IGitSettingsInitializer _gitSettingsInitializer;

    public PatcherFactory(
        ILifetimeScope scope,
        IGitSettingsInitializer gitSettingsInitializer)
    {
        _scope = scope;
        _gitSettingsInitializer = gitSettingsInitializer;
    }
        
    public PatcherVm Get(PatcherSettings settings)
    {
        return settings switch
        {
            GithubPatcherSettings git => GetGitPatcher(git),
            SolutionPatcherSettings soln => GetSolutionPatcher(soln),
            CliPatcherSettings cli => GetCliPatcher(cli),
            _ => throw new NotImplementedException(),
        };
    }

    private void RegisterId(ContainerBuilder c)
    {
        var inject = new PatcherIdInjection(Guid.NewGuid());
        c.RegisterInstance(inject)
            .AsSelf()
            .AsImplementedInterfaces();
    }

    public GitPatcherVm GetGitPatcher(GithubPatcherSettings? settings = null)
    {
        var patcherScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
        {
            RegisterId(c);
            c.RegisterModule<GuiGitPatcherModule>();
            settings = _gitSettingsInitializer.Get(settings);
            c.RegisterInstance(settings)
                .AsSelf()
                .AsImplementedInterfaces();
        });
        var patcher = patcherScope.Resolve<GitPatcherVm>();
        patcherScope.DisposeWith(patcher);
        return patcher;
    }

    public SolutionPatcherVm GetSolutionPatcher(SolutionPatcherSettings settings)
    {
        var patcherScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
        {
            RegisterId(c);
            c.RegisterModule<GuiSolutionPatcherModule>();
            c.RegisterInstance(settings)
                .AsSelf()
                .AsImplementedInterfaces();
        });
        var patcher = patcherScope.Resolve<SolutionPatcherVm>();
        patcherScope.DisposeWith(patcher);
        return patcher;
    }

    public CliPatcherVm GetCliPatcher(CliPatcherSettings settings)
    {
        var patcherScope = _scope.BeginLifetimeScope(LifetimeScopes.PatcherNickname, c =>
        {
            RegisterId(c);
            c.RegisterModule<GuiCliPatcherModule>();
            c.RegisterInstance(settings)
                .AsSelf();
        });
        var patcher = patcherScope.Resolve<CliPatcherVm>();
        patcherScope.DisposeWith(patcher);
        return patcher;
    }
}