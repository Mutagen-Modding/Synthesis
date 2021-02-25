using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt8SettingsVM : BasicSettingsVM<byte>
    {
        public UInt8SettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public UInt8SettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override byte Get(JsonElement property) => property.GetByte();

        public override byte GetDefault() => default(byte);

        public override SettingsNodeVM Duplicate() => new UInt8SettingsVM(Meta, DefaultValue);
    }
}
