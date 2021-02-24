using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public record MemberName(string DisplayName, string DiskName)
    {
        public static readonly MemberName Empty = new MemberName(string.Empty, string.Empty);
    }
}
