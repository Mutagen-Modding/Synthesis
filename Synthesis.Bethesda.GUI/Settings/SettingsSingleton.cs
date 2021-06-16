using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI.Settings
{
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
            IGuiPaths guiPaths,
            IPaths paths)
        {
            SynthesisGuiSettings? guiSettings = null;
            PipelineSettings? pipeSettings = null;
            Task.WhenAll(
                Task.Run(async () =>
                {
                    if (fileSystem.File.Exists(guiPaths.GuiSettingsPath))
                    {
                        guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(await fileSystem.File.ReadAllTextAsync(guiPaths.GuiSettingsPath), Execution.Constants.JsonSettings)!;
                    }
                }),
                Task.Run(async () =>
                {
                    if (fileSystem.File.Exists(paths.SettingsFileName))
                    {
                        pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(await fileSystem.File.ReadAllTextAsync(paths.SettingsFileName), Execution.Constants.JsonSettings)!;
                    }
                })
            ).Wait();
            Gui = guiSettings ?? new();
            Pipeline = pipeSettings ?? new();
        }
    }
}