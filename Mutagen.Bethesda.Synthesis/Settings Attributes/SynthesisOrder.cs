using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.Settings
{
    /// <summary>
    /// https://stackoverflow.com/questions/9062235/get-properties-in-order-of-declaration-using-reflection
    /// Hopefully will not be needed to specify eventually with something like a Fody plugin?
    /// </summary>
    [AttributeUsage(
      AttributeTargets.Field | AttributeTargets.Property,
      AllowMultiple = false)]
    public class SynthesisOrder : Attribute
    {
        public int Order { get; }

        public SynthesisOrder([CallerLineNumber] int order = 0)
        {
            Order = order;
        }
    }
}
