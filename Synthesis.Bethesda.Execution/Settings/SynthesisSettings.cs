using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class SynthesisSettings
    {
        public string SelectedProfile = string.Empty;
        public List<SynthesisProfile> Profiles = new List<SynthesisProfile>();
    }
}
