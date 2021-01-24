using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class ModKeySettingsVM : BasicSettingsVM<ModKey>
    {
        public ModKeySettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public ModKeySettingsVM()
            : base(string.Empty, default)
        {
        }

        public override SettingsNodeVM Duplicate() => new ModKeySettingsVM(MemberName, DefaultValue);

        public override ModKey Get(JsonElement property)
        {
            return ModKey.FromNameAndExtension(property.GetString());
        }

        public override ModKey GetDefault() => ModKey.Null;
    }
}
