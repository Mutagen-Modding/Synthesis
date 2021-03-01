using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt64SettingsVM : BasicSettingsVM<ulong>
    {
        public UInt64SettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public UInt64SettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override ulong Get(JsonElement property) => property.GetUInt64();

        public override ulong GetDefault() => default(ulong);

        public override SettingsNodeVM Duplicate() => new UInt64SettingsVM(Meta, DefaultValue);
    }
}
