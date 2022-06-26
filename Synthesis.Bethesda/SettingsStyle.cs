using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda;

public record SettingsConfiguration(
    SettingsStyle Style,
    ReflectionSettingsConfig[] Targets);

public enum SettingsStyle
{
    None,
    Open,
    Host,
    SpecifiedClass
}