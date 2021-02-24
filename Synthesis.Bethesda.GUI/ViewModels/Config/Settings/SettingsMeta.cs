using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public record SettingsMeta(string DisplayName, string DiskName, string? Tooltip)
    {
        public static readonly SettingsMeta Empty = new SettingsMeta(string.Empty, string.Empty, null);
    }
}
