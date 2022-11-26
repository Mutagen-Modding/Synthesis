namespace Mutagen.Bethesda.Synthesis;

public interface ISettingsDataConfiguration
{
    /// <summary>
    /// Path to the supplemental data folder dedicated to storing patcher specific user settings/files
    /// </summary>
    string ExtraSettingsDataPath { get; }

    /// <summary>
    /// Path to the internal data folder dedicated to storing patcher specific files hidden from user
    /// </summary>
    string? InternalDataPath { get; }

    /// <summary>
    /// Path to the default data folder as defined by the patcher's source code
    /// </summary>
    string? DefaultSettingsDataPath { get; }
}