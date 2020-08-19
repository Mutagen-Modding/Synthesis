using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class SynthesisGuiSettings
    {
        public SynthesisSettings ExecutableSettings = new SynthesisSettings();
        public bool ShowHelp = true;
        public OpenWithEnum OpenWithProgram = OpenWithEnum.VisualStudio;
        public string MainRepositoryFolder = string.Empty;
    }
}
