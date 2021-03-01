using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int16SettingsVM : BasicSettingsVM<short>
    {
        public Int16SettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public Int16SettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override short Get(JsonElement property) => property.GetInt16();

        public override short GetDefault() => default(short);

        public override SettingsNodeVM Duplicate() => new Int16SettingsVM(Meta, DefaultValue);
    }
}
