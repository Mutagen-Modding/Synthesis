using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.Json.Pipeline;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.Json;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI.Settings;

public interface ISettingsSingleton
{
    ISynthesisGuiSettings Gui { get; }
    IPipelineSettings Pipeline { get; }
}

public class SettingsSingleton : ISettingsSingleton
{
    public ISynthesisGuiSettings Gui { get; }
    public IPipelineSettings Pipeline { get; }

    public SettingsSingleton(
        IFileSystem fileSystem,
        IGuiSettingsPath guiPaths,
        IGuiSettingsImporter guiSettingsImporter,
        IPipelineSettingsImporter pipelineSettingsImporter,
        IPipelineSettingsPath paths)
    {
        ISynthesisGuiSettings? guiSettings = null;
        IPipelineSettings? pipeSettings = null;
        Task.WhenAll(
            Task.Run(async () =>
            {
                if (fileSystem.File.Exists(guiPaths.Path))
                {
                    guiSettings = guiSettingsImporter.Import(guiPaths.Path);
                }
            }),
            Task.Run(async () =>
            {
                if (fileSystem.File.Exists(paths.Path))
                {
                    pipeSettings = pipelineSettingsImporter.Import(paths.Path);
                }
            })
        ).Wait();
        Gui = guiSettings ?? new SynthesisGuiSettings();
        Pipeline = pipeSettings ?? new PipelineSettings();
    }
}

public class SettingsLoader : IStartupTask
{
    private readonly Lazy<ISettingsSingleton> _settings;

    public SettingsLoader(
        Lazy<ISettingsSingleton> settings)
    {
        _settings = settings;
    }
        
    public void Start()
    {
        _settings.Value.GetType();
    }
}