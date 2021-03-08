using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public record FieldMeta(
        string DisplayName,
        string DiskName,
        string? Tooltip,
        ReflectionSettingsVM MainVM,
        SettingsNodeVM? Parent,
        bool IsPassthrough)
    {
        public static readonly FieldMeta Empty = new(string.Empty, string.Empty, null, null!, null, false);
    }
}
