using System;
using System.Linq;
using DynamicData;
using Mutagen.Bethesda;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Settings;

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
            var patcherFactory = profile.Container.GetInstance<IPatcherFactory>();
            profile.Patchers.AddRange(settings.Patchers.Select(patcherFactory.Get));
            return profile;
        }

        public ProfileVM Get(GameRelease release, string id, string nickname)
        {
            var scope = Inject.Container.CreateChildContainer();
            var ident = scope.GetInstance<ProfileIdentifier>();
            ident.ID = id;
            ident.Release = release;
            ident.Nickname = nickname;
            scope.GetInstance<IContainerTracker>().Container = scope;
            var profile = scope
                .With(scope)
                .With(ident)
                .GetInstance<ProfileVM>();
            scope.DisposeWith(profile);
            return profile;
        }
    }
}