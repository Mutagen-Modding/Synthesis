using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.Settings
{
    [AttributeUsage(
      AttributeTargets.Field | AttributeTargets.Property,
      AllowMultiple = false)]
    public class SynthesisTooltip : Attribute
    {
        public string Text { get; }

        public SynthesisTooltip(string text)
        {
            Text = text;
        }
    }
}
