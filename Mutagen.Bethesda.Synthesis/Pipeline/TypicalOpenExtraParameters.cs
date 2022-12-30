using System.IO.Abstractions;
using Mutagen.Bethesda.Strings;

namespace Mutagen.Bethesda.Synthesis.Pipeline;

public record TypicalOpenExtraParameters
{
    public Language TargetLanguage { get; init; } = Language.English;
    public bool Localize { get; init; }
    public bool UseUtf8ForEmbeddedStrings { get; init; }
}