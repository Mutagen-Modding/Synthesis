using System.Linq;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Serilog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Registers;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public interface IProfileFactory
    {
        ProfileVM Get(ISynthesisProfile settings);
        ProfileVM Get(GameRelease release, string id, string nickname);
    }

    public class ProfileFactory : IProfileFactory
    {
        private readonly ILogger _Logger;

        public ProfileFactory(ILogger logger)
        {
            _Logger = logger;
        }
        
        public ProfileVM Get(ISynthesisProfile settings)
        {
            _Logger.Information("Loading {Release} Profile {Nickname} with ID {ID}", settings.TargetRelease, settings.Nickname, settings.ID);
            var profile = Get(settings.TargetRelease, settings.ID, settings.Nickname);
            profile.Versioning.MutagenVersioning = settings.MutagenVersioning;
            profile.Versioning.ManualMutagenVersion = settings.MutagenManualVersion;
            profile.Versioning.SynthesisVersioning = settings.SynthesisVersioning;
            profile.Versioning.ManualSynthesisVersion = settings.SynthesisManualVersion;
            profile.DataFolderOverride.DataPathOverride = settings.DataPathOverride;
            profile.ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
            profile.LockSetting.Lock = settings.LockToCurrentVersioning;
            profile.SelectedPersistenceMode = settings.Persistence;
            profile.Patchers.AddRange(settings.Patchers.Select(profile.PatcherFactory.Get));
            return profile;
        }

        public ProfileVM Get(GameRelease release, string id, string nickname)
        {
            _Logger.Information("Creating {Release} Profile {Nickname} with ID {ID}", release, nickname, id);
            return InternalGet(release, id, nickname);
        }

        public ProfileVM InternalGet(GameRelease release, string id, string nickname)
        {
            var ident = new ProfileIdentifier(Inject.Container.CreateChildContainer())
            {
                ID = id,
                Release = release,
                Nickname = nickname
            };
            ident.Container.Configure(cfg =>
            {
                cfg.For<IProfileIdentifier>().Use(ident);
                cfg.For<IGameReleaseContext>().Use(ident);
                cfg.AddRegistry<ProfileRegister>();
            });
            return ident.Container.GetInstance<ProfileVM>();
        }
    }
}