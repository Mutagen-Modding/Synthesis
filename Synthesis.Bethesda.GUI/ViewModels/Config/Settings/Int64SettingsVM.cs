using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int64SettingsVM : BasicSettingsVM<long>
    {
        public Int64SettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public Int64SettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override long Get(JsonElement property) => property.GetInt64();

        public override long GetDefault() => default(long);

        public override SettingsNodeVM Duplicate() => new Int64SettingsVM(Meta, DefaultValue);
    }
}
