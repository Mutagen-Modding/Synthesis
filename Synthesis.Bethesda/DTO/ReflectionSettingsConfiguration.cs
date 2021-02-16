using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.DTO
{
    public record ReflectionSettingsConfig(string TypeName, string Nickname, string Path);
    public record ReflectionSettingsConfigs(ReflectionSettingsConfig[] Configs);
}
