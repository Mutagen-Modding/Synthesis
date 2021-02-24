using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class BoolSettingsVM : BasicSettingsVM<bool>
    {
        public BoolSettingsVM(SettingsMeta memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public BoolSettingsVM()
            : base(SettingsMeta.Empty, default)
        {
        }

        public override bool Get(JsonElement property) => property.GetBoolean();

        public override bool GetDefault() => default(bool);

        public override SettingsNodeVM Duplicate() => new BoolSettingsVM(Meta, DefaultValue);
    }
}
