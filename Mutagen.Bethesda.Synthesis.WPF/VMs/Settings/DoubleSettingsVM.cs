using System.Text.Json;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class DoubleSettingsVM : BasicSettingsVM<double>
    {
        public DoubleSettingsVM(FieldMeta meta, object? defaultVal)
            : base(meta, defaultVal)
        {
        }

        public DoubleSettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override double Get(JsonElement property) => property.GetDouble();

        public override double GetDefault() => default(double);

        public override SettingsNodeVM Duplicate() => new DoubleSettingsVM(Meta, DefaultValue);
    }
}
