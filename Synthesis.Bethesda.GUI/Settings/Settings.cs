using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI
{
    public interface ISettings
    {
        SynthesisGuiSettings Gui { get; }
        PipelineSettings Pipeline { get; }
    }

    public class Settings : ISettings
    {
        public SynthesisGuiSettings Gui { get; }
        public PipelineSettings Pipeline { get; }

        public Settings()
        {
            SynthesisGuiSettings? guiSettings = null;
            PipelineSettings? pipeSettings = null;
            Task.WhenAll(
                Task.Run(async () =>
                {
                    if (File.Exists(Paths.GuiSettingsPath))
                    {
                        guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(await File.ReadAllTextAsync(Paths.GuiSettingsPath), Execution.Constants.JsonSettings)!;
                    }
                }),
                Task.Run(async () =>
                {
                    if (File.Exists(Execution.Paths.SettingsFileName))
                    {
                        pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(await File.ReadAllTextAsync(Execution.Paths.SettingsFileName), Execution.Constants.JsonSettings)!;
                    }
                })
            ).Wait();
            Gui = guiSettings ?? new();
            Pipeline = pipeSettings ?? new();
        }
    }
}