using Mutagen.Bethesda.Strings;
using Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.Pipeline;

public record TypicalOpenExtraParameters
{
    public Language TargetLanguage { get; init; } = Language.English;
    public bool Localize { get; init; }
    public bool UseUtf8ForEmbeddedStrings { get; init; }
    public float? HeaderVersionOverride { get; init; }
    public FormIDRangeMode FormIDRangeMode { get; init; }
}