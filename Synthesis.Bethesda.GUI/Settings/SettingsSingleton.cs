using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Settings;

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

        public SettingsSingleton(IFileSystem fileSystem)
        {
            SynthesisGuiSettings? guiSettings = null;
            PipelineSettings? pipeSettings = null;
            Task.WhenAll(
                Task.Run(async () =>
                {
                    if (fileSystem.File.Exists(Paths.GuiSettingsPath))
                    {
                        guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(await fileSystem.File.ReadAllTextAsync(Paths.GuiSettingsPath), Execution.Constants.JsonSettings)!;
                    }
                }),
                Task.Run(async () =>
                {
                    if (fileSystem.File.Exists(Execution.Paths.SettingsFileName))
                    {
                        pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(await fileSystem.File.ReadAllTextAsync(Execution.Paths.SettingsFileName), Execution.Constants.JsonSettings)!;
                    }
                })
            ).Wait();
            Gui = guiSettings ?? new();
            Pipeline = pipeSettings ?? new();
        }
    }
}