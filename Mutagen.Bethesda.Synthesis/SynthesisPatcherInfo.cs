using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class SynthesisPatcherInfo
    {
        public string? Description { get; set; }
        public string? Nickname { get; set; }
        public bool HideByDefault { get; set; } = false;
    }
}
