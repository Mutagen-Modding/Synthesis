using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public record FieldMeta(string DisplayName, string DiskName, string? Tooltip)
    {
        public static readonly FieldMeta Empty = new FieldMeta(string.Empty, string.Empty, null);
    }
}
