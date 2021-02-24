using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DoubleSettingsVM : BasicSettingsVM<double>
    {
        public DoubleSettingsVM(SettingsMeta meta, object? defaultVal)
            : base(meta, defaultVal)
        {
        }

        public DoubleSettingsVM()
            : base(SettingsMeta.Empty, default)
        {
        }

        public override double Get(JsonElement property) => property.GetDouble();

        public override double GetDefault() => default(double);

        public override SettingsNodeVM Duplicate() => new DoubleSettingsVM(Meta, DefaultValue);
    }
}
