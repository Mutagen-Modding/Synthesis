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
                    GithubPatcherSettings git => profile.Container
                        .With(git)
                        .GetInstance<GitPatcherVM>(),
                    SolutionPatcherSettings soln => profile.Container
                        .With(soln)
                        .GetInstance<SolutionPatcherVM>(),
                    CliPatcherSettings cli =>profile.Container
                        .With(cli)
                        .GetInstance<CliPatcherVM>(),
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
            var profile = scope
                .With(scope)
                .With(ident)
                .GetInstance<ProfileVM>();
            scope.DisposeWith(profile);
            return profile;
        }
    }
}