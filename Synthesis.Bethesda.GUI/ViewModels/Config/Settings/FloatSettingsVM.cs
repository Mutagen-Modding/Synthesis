using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class FloatSettingsVM : BasicSettingsVM<float>
    {
        public FloatSettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public FloatSettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override float Get(JsonElement property) => property.GetSingle();

        public override float GetDefault() => default(float);

        public override SettingsNodeVM Duplicate() => new FloatSettingsVM(Meta, DefaultValue);
    }
}
