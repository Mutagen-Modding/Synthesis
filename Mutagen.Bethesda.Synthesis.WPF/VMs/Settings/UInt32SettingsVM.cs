using System.Text.Json;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class UInt32SettingsVM : BasicSettingsVM<uint>
    {
        public UInt32SettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public UInt32SettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override uint Get(JsonElement property) => property.GetUInt32();

        public override uint GetDefault() => default(uint);

        public override SettingsNodeVM Duplicate() => new UInt32SettingsVM(Meta, DefaultValue);
    }
}
