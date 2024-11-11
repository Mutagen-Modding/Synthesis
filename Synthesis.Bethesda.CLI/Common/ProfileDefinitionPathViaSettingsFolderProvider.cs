using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.Common;

public class ProfileDefinitionPathViaSettingsFolderProvider : IProfileDefinitionPathProvider
{
    private readonly ISettingsFolderProvider _settingsFolderProvider;
    private readonly IPipelineSettingsPath _pipelineSettingsPath;

    public FilePath Path => System.IO.Path.Combine(_settingsFolderProvider.SettingsFolder, _pipelineSettingsPath.Name);

    public ProfileDefinitionPathViaSettingsFolderProvider(
        ISettingsFolderProvider settingsFolderProvider,
        IPipelineSettingsPath pipelineSettingsPath)
    {
        _settingsFolderProvider = settingsFolderProvider;
        _pipelineSettingsPath = pipelineSettingsPath;
    }
}