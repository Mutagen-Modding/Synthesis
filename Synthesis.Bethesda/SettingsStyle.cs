using Synthesis.Bethesda.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda
{
    public record SettingsConfiguration(
        SettingsStyle Style,
        ReflectionSettingsConfig[] Targets);

    public enum SettingsStyle
    {
        None,
        Open,
        Host,
        SpecifiedClass
    }
}
