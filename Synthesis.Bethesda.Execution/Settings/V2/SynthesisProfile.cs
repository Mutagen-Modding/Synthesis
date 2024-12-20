using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Strings;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Settings.V2;

[ExcludeFromCodeCoverage]
public class SynthesisProfile : ISynthesisProfileSettings
{
    public string Nickname { get; set; } = string.Empty;
    public string ID { get; set; } = string.Empty;
    public GameRelease TargetRelease { get; set; }
    public List<PatcherGroupSettings> Groups { get; set; } = new();
    public NugetVersioningEnum MutagenVersioning { get; set; } = NugetVersioningEnum.Manual;
    public string? MutagenManualVersion { get; set; }
    public NugetVersioningEnum SynthesisVersioning { get; set; } = NugetVersioningEnum.Manual;
    public string? SynthesisManualVersion { get; set; }
    public string? DataPathOverride { get; set; }
    public bool ConsiderPrereleaseNugets { get; set; }
    public bool LockToCurrentVersioning { get; set; }
    public PersistenceMode FormIdPersistence { get; set; } = PersistenceMode.None;
    public bool IgnoreMissingMods { get; set; }
    public bool Localize { get; set; } = false;
    public Language TargetLanguage { get; set; } = Language.English;

    GameRelease IGameReleaseContext.Release => TargetRelease;
    public bool UseUtf8ForEmbeddedStrings { get; set; }
    public FormIDRangeMode FormIDRangeMode { get; set; } = FormIDRangeMode.Auto;
    public float? HeaderVersionOverride { get; set; }
    public bool ExportAsMasterFiles { get; set; }
    public bool MasterStyleFallbackEnabled { get; set; }
    public MasterStyle MasterStyle { get; set; }
    string IProfileNameProvider.Name => Nickname;
}