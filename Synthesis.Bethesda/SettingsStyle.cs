using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda
{
    public record SettingsTarget(SettingsStyle Style, string? SettingsType);

    public enum SettingsStyle
    {
        None,
        Open,
        SpecifiedClass
    }
}
