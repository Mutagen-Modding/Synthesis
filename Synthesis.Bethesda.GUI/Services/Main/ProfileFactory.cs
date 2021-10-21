using System.Linq;
using Noggog;
using Autofac;
using DynamicData;
using Mutagen.Bethesda;
using Serilog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Services.Profile;
using Synthesis.Bethesda.GUI.Services.Versioning;
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
        private readonly ILifetimeScope _scope;
        private readonly INewestLibraryVersionsVm _newestLibraryVersionsVm;
        private readonly ILogger _logger;

        public ProfileFactory(
            ILifetimeScope scope,
            INewestLibraryVersionsVm newestLibraryVersionsVm,
            ILogger logger)
        {
            _scope = scope;
            _newestLibraryVersionsVm = newestLibraryVersionsVm;
            _logger = logger;
        }
        
        public ProfileVm Get(ISynthesisProfileSettings settings)
        {
            _logger.Information("Loading {Release} Profile {Nickname} with ID {ID}", settings.TargetRelease, settings.Nickname, settings.ID);
            var scope = _scope.BeginLifetimeScope(
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
            var factory = scope.Resolve<IGroupFactory>();
            var newGroup = scope.Resolve<INewGroupCreator>();

            scope.DisposeWith(profile);
            profile.Versioning.MutagenVersioning = settings.MutagenVersioning;
            profile.Versioning.ManualMutagenVersion = settings.MutagenManualVersion;
            profile.Versioning.SynthesisVersioning = settings.SynthesisVersioning;
            profile.Versioning.ManualSynthesisVersion = settings.SynthesisManualVersion;
            profile.DataFolderOverride.DataPathOverride = settings.DataPathOverride;
            profile.IgnoreMissingMods = settings.IgnoreMissingMods;
            profile.ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
            profile.LockSetting.Lock = settings.LockToCurrentVersioning;
            profile.SelectedPersistenceMode = settings.FormIdPersistence;

            profile.Groups.AddRange(settings.Groups.Select(x => factory.Get(x)));

            if (profile.Groups.Count == 0)
            {
                profile.Groups.Add(newGroup.Get());
            }
            
            return profile;
        }

        public ProfileVm Get(GameRelease release, string id, string nickname)
        {
            _logger.Information("Creating {Release} Profile {Nickname} with ID {ID}", release, nickname, id);
            var scope = _scope.BeginLifetimeScope(
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
            var profile = scope.Resolve<ProfileVm>();
            profile.Versioning.ManualMutagenVersion = _newestLibraryVersionsVm.NewestMutagenVersion;
            profile.Versioning.ManualMutagenVersion = _newestLibraryVersionsVm.NewestMutagenVersion;
            var newGroup = scope.Resolve<INewGroupCreator>();

            scope.DisposeWith(profile);
            var group = newGroup.Get();
            group.Name = Constants.SynthesisName;
            profile.Groups.Add(group);
            return profile;
        }
    }
}