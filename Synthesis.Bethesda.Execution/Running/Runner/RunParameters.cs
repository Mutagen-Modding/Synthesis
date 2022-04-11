using Mutagen.Bethesda.Strings;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public record RunParameters(
    Language TargetLanguage,
    bool Localize,
    PersistenceMode PersistenceMode,
    string? PersistencePath);