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
    public class SynthesisDiskName : Attribute
    {
        public string Name { get; }

        public SynthesisDiskName(string name)
        {
            Name = name;
        }
    }
}
