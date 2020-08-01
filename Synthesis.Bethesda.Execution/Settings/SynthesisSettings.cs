using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class SynthesisSettings
    {
        public List<PatcherSettings> Patchers = new List<PatcherSettings>();
    }
}
