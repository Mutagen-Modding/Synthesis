using Noggog;

namespace Synthesis.Bethesda.Execution.Settings;

public interface ISettingsFolderProvider
{
    DirectoryPath SettingsFolder { get; }
}