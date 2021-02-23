using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.Settings
{
    [AttributeUsage(
      AttributeTargets.Field | AttributeTargets.Property,
      AllowMultiple = false)]
    public class SynthesisSettingName : Attribute
    {
        public string Name;

        public SynthesisSettingName(string name)
        {
            Name = name;
        }
    }
}
