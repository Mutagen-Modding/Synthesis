using Noggog;

namespace Mutagen.Bethesda.Synthesis;

public interface ISettingsDataConfiguration
{
    /// <summary>
    /// Path to the supplemental data folder dedicated to storing patcher specific user settings/files <br/>
    /// To get a file, consider using (Try)RetrieveConfigFile as a convenience method that accesses this path.
    /// </summary>
    DirectoryPath? ExtraSettingsDataPath { get; }

    /// <summary>
    /// Path to the internal data folder dedicated to storing patcher specific files hidden from user <br/>
    /// To get a file, consider using (Try)RetrieveInternalFile as a convenience method that accesses this path.
    /// </summary>
    DirectoryPath? InternalDataPath { get; }

    /// <summary>
    /// Path to the default data folder as defined by the patcher's source code
    /// </summary>
    DirectoryPath? DefaultSettingsDataPath { get; }
}