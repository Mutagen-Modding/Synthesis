using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DecimalSettingsVM : BasicSettingsVM<decimal>
    {
        public DecimalSettingsVM(SettingsMeta meta, object? defaultVal)
            : base(meta, defaultVal)
        {
        }

        public DecimalSettingsVM()
            : base(SettingsMeta.Empty, default)
        {
        }

        public override decimal Get(JsonElement property) => property.GetDecimal();

        public override decimal GetDefault() => default(decimal);

        public override SettingsNodeVM Duplicate() => new DecimalSettingsVM(Meta, DefaultValue);
    }
}
