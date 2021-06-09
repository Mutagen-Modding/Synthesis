using System;
using System.Linq;
using DynamicData;
using Mutagen.Bethesda;
using Noggog.WPF;
using Serilog;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.Temporary;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IProfileFactory
    {
        ProfileVM Get(SynthesisProfile settings);
        ProfileVM Get(GameRelease release, string id, string nickname);
    }

    public class ProfileFactory : IProfileFactory
    {
        public ProfileVM Get(SynthesisProfile settings)
        {
            var profile = Get(settings.TargetRelease, settings.ID, settings.Nickname);
            profile.Versioning.MutagenVersioning = settings.MutagenVersioning;
            profile.Versioning.ManualMutagenVersion = settings.MutagenManualVersion;
            profile.Versioning.SynthesisVersioning = settings.SynthesisVersioning;
            profile.Versioning.ManualSynthesisVersion = settings.SynthesisManualVersion;
            profile.DataFolderOverride.DataPathOverride = settings.DataPathOverride;
            profile.ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
            profile.LockSetting.Lock = settings.LockToCurrentVersioning;
            profile.SelectedPersistenceMode = settings.Persistence;
            profile.Patchers.AddRange(settings.Patchers.Select<PatcherSettings, PatcherVM>(p =>
            {
                return p switch
                {
                    GithubPatcherSettings git => new GitPatcherVM(
                        profile.Container.GetInstance<ProfileIdentifier>(),
                        profile.Container.GetInstance<ProfileDirectories>(),
                        profile.Container.GetInstance<ProfileLoadOrder>(),
                        profile.Container.GetInstance<ProfilePatchersList>(),
                        profile.Container.GetInstance<ProfileVersioning>(),
                        profile.Container.GetInstance<ProfileDataFolder>(),
                        profile.Container.GetInstance<IRemovePatcherFromProfile>(),
                        profile.Container.GetInstance<INavigateTo>(),
                        profile.Container.GetInstance<ICheckOrCloneRepo>(),
                        profile.Container.GetInstance<IProvideRepositoryCheckouts>(),
                        profile.Container.GetInstance<ICheckoutRunnerRepository>(),
                        profile.Container.GetInstance<ICheckRunnability>(),
                        profile.Container.GetInstance<IProfileDisplayControllerVm>(),
                        profile.Container.GetInstance<IConfirmationPanelControllerVm>(),
                        profile.Container.GetInstance<ILockToCurrentVersioning>(),
                        profile.Container.GetInstance<IBuild>(),
                        git),
                    SolutionPatcherSettings soln => new SolutionPatcherVM(
                        profile.Container.GetInstance<ProfileLoadOrder>(),
                        profile.Container.GetInstance<IRemovePatcherFromProfile>(),
                        profile.Container.GetInstance<IProvideInstalledSdk>(),
                        profile.Container.GetInstance<IProfileDisplayControllerVm>(),
                        profile.Container.GetInstance<IConfirmationPanelControllerVm>(),
                        soln),
                    CliPatcherSettings cli => new CliPatcherVM(
                        profile.Container.GetInstance<IRemovePatcherFromProfile>(),
                        profile.Container.GetInstance<IProfileDisplayControllerVm>(),
                        profile.Container.GetInstance<IConfirmationPanelControllerVm>(),
                        profile.Container.GetInstance<IShowHelpSetting>(),
                        cli),
                    _ => throw new NotImplementedException(),
                };
            }));
            return profile;
        }

        public ProfileVM Get(GameRelease release, string id, string nickname)
        {
            var scope = Inject.Container.CreateChildContainer();
            var ident = scope.GetInstance<ProfileIdentifier>();
            ident.ID = id;
            ident.Release = release;
            ident.Nickname = nickname;
            scope.GetInstance<ContainerTracker>().Container = scope;
            var profile = new ProfileVM(
                scope, 
                scope.GetInstance<ProfilePatchersList>(),
                scope.GetInstance<ProfileDataFolder>(),
                scope.GetInstance<PatcherInitializationVM>(),
                ident,
                scope.GetInstance<ProfileLoadOrder>(),
                scope.GetInstance<ProfileDirectories>(),
                scope.GetInstance<ProfileVersioning>(),
                scope.GetInstance<INavigateTo>(),
                scope.GetInstance<ILogger>());
            scope.DisposeWith(profile);
            return profile;
        }
    }
}