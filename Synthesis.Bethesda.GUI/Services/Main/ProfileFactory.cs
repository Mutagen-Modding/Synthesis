using System.Reactive.Linq;
using Noggog;
using Autofac;
using DynamicData;
using Mutagen.Bethesda;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Services.Profile;
using Synthesis.Bethesda.GUI.Services.Versioning;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface IProfileFactory
{
    ProfileVm Get(ISynthesisProfileSettings settings);
    ProfileVm Get(GameRelease release, string id, string nickname);
}

public class ProfileFactory : IProfileFactory
{
    private readonly ILifetimeScope _scope;
    private readonly INewestLibraryVersionsVm _newestLibraryVersions;
    private readonly ILogger _logger;

    public ProfileFactory(
        ILifetimeScope scope,
        INewestLibraryVersionsVm newestLibraryVersions,
        ILogger logger)
    {
        _scope = scope;
        _newestLibraryVersions = newestLibraryVersions;
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
            });
        var profile = scope.Resolve<ProfileVm>();
        var factory = scope.Resolve<IGroupFactory>();
        var newGroup = scope.Resolve<INewGroupCreator>();

        scope.DisposeWith(profile);
        profile.Versioning.MutagenVersioning = settings.MutagenVersioning;
        profile.Versioning.ManualMutagenVersion = settings.MutagenManualVersion;
        profile.Versioning.SynthesisVersioning = settings.SynthesisVersioning;
        profile.Versioning.ManualSynthesisVersion = settings.SynthesisManualVersion;
        profile.Overrides.DataPathOverride = settings.DataPathOverride;
        profile.IgnoreMissingMods = settings.IgnoreMissingMods;
        profile.ConsiderPrereleaseNugets = settings.ConsiderPrereleaseNugets;
        profile.LockSetting.Lock = settings.LockToCurrentVersioning;
        profile.SelectedPersistenceMode = settings.FormIdPersistence;
        profile.TargetLanguage = settings.TargetLanguage;
        profile.Localize = settings.Localize;
        profile.UseUtf8InEmbedded = settings.UseUtf8ForEmbeddedStrings;
        profile.FormIDRangeMode = settings.FormIDRangeMode;
        profile.HeaderVersionOverride = settings.HeaderVersionOverride;
        profile.MasterFile = settings.ExportAsMasterFiles;
        profile.MasterStyleFallbackEnabled = settings.MasterStyleFallbackEnabled;
        profile.MasterStyle = settings.MasterStyle;

        profile.Groups.AddRange(settings.Groups.Select(x => factory.Get(x)));

        if (profile.Groups.Count == 0)
        {
            var initialGroup = newGroup.Get();
            profile.Groups.Add(initialGroup);
            profile.DisplayController.SelectedObject = initialGroup;
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
        _newestLibraryVersions.WhenAnyValue(x => x.Versions)
            .Select(x => x.Normal.Mutagen)
            .TakeUntil(x => x != null)
            .Subscribe(x => profile.Versioning.ManualMutagenVersion = x)
            .DisposeWith(profile);
        _newestLibraryVersions.WhenAnyValue(x => x.Versions)
            .Select(x => x.Normal.Synthesis)
            .TakeUntil(x => x != null)
            .Subscribe(x => profile.Versioning.ManualSynthesisVersion = x)
            .DisposeWith(profile);
        var newGroup = scope.Resolve<INewGroupCreator>();

        scope.DisposeWith(profile);
        var initialGroup = newGroup.Get();
        initialGroup.Name = Constants.SynthesisName;
        profile.Groups.Add(initialGroup);
        profile.DisplayController.SelectedObject = initialGroup;
        
        // Starfield requires masters
        profile.MasterFile = release == GameRelease.Starfield;
        
        return profile;
    }
}