using System.Linq;
using Noggog;
using Autofac;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Serilog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Modules;
using Synthesis.Bethesda.GUI.Services.Profile;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.Services.Main
{
    public interface IProfileFactory
    {
        ProfileVm Get(ISynthesisProfileSettings settings);
        ProfileVm Get(GameRelease release, string id, string nickname);
    }

    public class ProfileFactory : IProfileFactory
    {
        private readonly ILifetimeScope _Scope;
        private readonly ILogger _Logger;

        public ProfileFactory(
            ILifetimeScope scope,
            ILogger logger)
        {
            _Scope = scope;
            _Logger = logger;
        }
        
        public ProfileVm Get(ISynthesisProfileSettings settings)
        {
            _Logger.Information("Loading {Release} Profile {Nickname} with ID {ID}", settings.TargetRelease, settings.Nickname, settings.ID);
            var scope = _Scope.BeginLifetimeScope(
                LifetimeScopes.ProfileNickname, 
                cfg =>
                {
                    cfg.RegisterInstance(settings)
                        .AsSelf()
                        .AsImplementedInterfaces();
                    
                    cfg.RegisterType<ProfileLogDecorator>()
                        .AsImplementedInterfaces()
                        .SingleInstance();
                });
            var profile = scope.Resolve<ProfileVm>();
            
            scope.DisposeWith(profile);
            profile.Versioning.MutagenVersioning = settings.MutagenVersioning;
            profile.Versioning.ManualMutagenVersion = settings.MutagenManualVersion;
            profile.Versioning.SynthesisVersioning = settings.SynthesisVersioning;
            profile.Versioning.ManualSynthesisVersion = settings.SynthesisManualVersion;
            profile.DataFolderOverride.DataPathOverride = settings.DataPathOverride;
            profile.IgnoreMissingMods = settings.IgnoreMissingMods;
            profile.ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
            profile.LockSetting.Lock = settings.LockToCurrentVersioning;
            profile.SelectedPersistenceMode = settings.Persistence;

            var factory = scope.Resolve<IPatcherFactory>();
            profile.Patchers.AddRange(settings.Patchers.Select(x => factory.Get(x)));
            return profile;
        }

        public ProfileVm Get(GameRelease release, string id, string nickname)
        {
            _Logger.Information("Creating {Release} Profile {Nickname} with ID {ID}", release, nickname, id);
            var scope = _Scope.BeginLifetimeScope(
                LifetimeScopes.ProfileNickname,
                cfg =>
                {
                    cfg.RegisterInstance(
                            new SynthesisProfile()
                            {
                                Nickname = nickname,
                                ID = id,
                                TargetRelease = release
                            })
                        .AsImplementedInterfaces();
                });
            var ret = scope.Resolve<ProfileVm>();
            scope.DisposeWith(ret);
            return ret;
        }
    }
}