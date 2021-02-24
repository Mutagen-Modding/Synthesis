using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class StringSettingsVM : BasicSettingsVM<string>
    {
        public StringSettingsVM(SettingsMeta memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public StringSettingsVM()
            : base(SettingsMeta.Empty, default)
        {
        }

        public override string Get(JsonElement property) => property.GetString() ?? string.Empty;

        public override string GetDefault() => string.Empty;

        public override SettingsNodeVM Duplicate() => new StringSettingsVM(Meta, DefaultValue);
    }
}
