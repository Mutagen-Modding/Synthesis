using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis.Settings
{
    public class SynthesisSettings
    {
        public List<PatcherSettings> Patchers = new List<PatcherSettings>();
    }
}
