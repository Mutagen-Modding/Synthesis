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
        SynthesisGuiSettings Gui { get; set;}
        PipelineSettings Pipeline { get; set; }
    }

    public class Settings : ISettings
    {
        public SynthesisGuiSettings Gui { get; set; } = null!;
        public PipelineSettings Pipeline { get; set; } = null!;
    }
}