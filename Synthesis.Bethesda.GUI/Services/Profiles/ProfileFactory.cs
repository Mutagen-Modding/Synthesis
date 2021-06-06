using System;
using System.Linq;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Mutagen.Bethesda;
using Noggog.WPF;
using Serilog;
using SimpleInjector;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Profiles
{
    public interface IProfileFactory
    {
        ProfileVM Get(SynthesisProfile settings);
        ProfileVM Get(GameRelease release, string id);
    }

    public class ProfileFactory : IProfileFactory
    {
        private readonly PatcherInitializationVM _Init;
        private readonly INavigateTo _Navigate;
        private readonly ILogger _Logger;

        public ProfileFactory(
            PatcherInitializationVM init,
            INavigateTo navigate,
            ILogger logger)
        {
            _Init = init;
            _Navigate = navigate;
            _Logger = logger;
        }
        
        public ProfileVM Get(SynthesisProfile settings)
        {
            var profile = Get(settings.TargetRelease, settings.ID);
            profile.Nickname = settings.Nickname;
            profile.MutagenVersioning = settings.MutagenVersioning;
            profile.ManualMutagenVersion = settings.MutagenManualVersion;
            profile.SynthesisVersioning = settings.SynthesisVersioning;
            profile.ManualSynthesisVersion = settings.SynthesisManualVersion;
            profile.DataPathOverride = settings.DataPathOverride;
            profile.ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
            profile.LockUpgrades = settings.LockToCurrentVersioning;
            profile.SelectedPersistenceMode = settings.Persistence;
            profile.Patchers.AddRange(settings.Patchers.Select<PatcherSettings, PatcherVM>(p =>
            {
                return p switch
                {
                    GithubPatcherSettings git => new GitPatcherVM(
                        profile, 
                        Inject.Scope.GetRequiredService<INavigateTo>(),
                        Inject.Scope.GetRequiredService<ICheckOrCloneRepo>(),
                        Inject.Scope.GetRequiredService<IProvideRepositoryCheckouts>(),
                        Inject.Scope.GetRequiredService<ICheckoutRunnerRepository>(),
                        Inject.Scope.GetRequiredService<ICheckRunnability>(),
                        Inject.Scope.GetRequiredService<IBuild>(),
                        git),
                    SolutionPatcherSettings soln => new SolutionPatcherVM(profile, soln),
                    CliPatcherSettings cli => new CliPatcherVM(
                        profile,
                        Inject.Scope.GetInstance<IShowHelpSetting>(),
                        cli),
                    _ => throw new NotImplementedException(),
                };
            }));
            return profile;
        }

        public ProfileVM Get(GameRelease release, string id)
        {
            var scope = new Scope(Inject.Container);
            var profile = new ProfileVM(scope, _Init, release, id, _Navigate, _Logger);
            scope.DisposeWith(profile);
            scope.GetInstance<IProfileTracker>().Profile = profile;
            return profile;
        }
    }
}