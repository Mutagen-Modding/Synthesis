using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.Settings
{
    /// <summary>
    /// Specifies a member to be displayed when the object is part of any summary areas, 
    /// such as when scoping a child setting and this object is being displayed in the drill down summary
    /// </summary>
    [AttributeUsage(
      AttributeTargets.Class,
      AllowMultiple = true)]
    public class SynthesisObjectNameMember : Attribute
    {
        public string Name { get; }

        public SynthesisObjectNameMember(string name)
        {
            Name = name;
        }
    }
}
