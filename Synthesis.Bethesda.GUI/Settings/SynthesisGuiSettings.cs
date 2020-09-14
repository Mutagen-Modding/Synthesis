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
        public bool OpenIdeAfterCreating = true;
        public IDE Ide = IDE.SystemDefault;
        public string MainRepositoryFolder = string.Empty;
    }
}
