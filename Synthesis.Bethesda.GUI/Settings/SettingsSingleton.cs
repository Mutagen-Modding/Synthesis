using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Main;

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
            IGuiSettingsPath guiPaths,
            IPipelineSettingsPath paths)
        {
            SynthesisGuiSettings? guiSettings = null;
            PipelineSettings? pipeSettings = null;
            Task.WhenAll(
                Task.Run(async () =>
                {
                    if (fileSystem.File.Exists(guiPaths.Path))
                    {
                        guiSettings = JsonConvert.DeserializeObject<SynthesisGuiSettings>(await fileSystem.File.ReadAllTextAsync(guiPaths.Path), Execution.Constants.JsonSettings)!;
                    }
                }),
                Task.Run(async () =>
                {
                    if (fileSystem.File.Exists(paths.Path))
                    {
                        pipeSettings = JsonConvert.DeserializeObject<PipelineSettings>(await fileSystem.File.ReadAllTextAsync(paths.Path), Execution.Constants.JsonSettings)!;
                    }
                })
            ).Wait();
            Gui = guiSettings ?? new();
            Pipeline = pipeSettings ?? new();
        }
    }
}