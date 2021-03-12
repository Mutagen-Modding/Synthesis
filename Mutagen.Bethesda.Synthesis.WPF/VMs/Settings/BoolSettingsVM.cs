using System.Text.Json;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class BoolSettingsVM : BasicSettingsVM<bool>
    {
        public BoolSettingsVM(FieldMeta fieldMeta, object? defaultVal)
            : base(fieldMeta, defaultVal)
        {
        }

        public BoolSettingsVM()
            : base(FieldMeta.Empty, default)
        {
        }

        public override bool Get(JsonElement property) => property.GetBoolean();

        public override bool GetDefault() => default(bool);

        public override SettingsNodeVM Duplicate() => new BoolSettingsVM(Meta, DefaultValue);
    }
}
