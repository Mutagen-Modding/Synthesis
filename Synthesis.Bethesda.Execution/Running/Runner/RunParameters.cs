using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Strings;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public record RunParameters(
    Language TargetLanguage,
    bool Localize,
    bool UseUtf8ForEmbeddedStrings,
    float? HeaderVersionOverride,
    FormIDRangeMode FormIDRangeMode,
    PersistenceMode PersistenceMode,
    string? PersistencePath,
    bool Master,
    bool MasterStyleFallbackEnabled,
    MasterStyle MasterStyle);