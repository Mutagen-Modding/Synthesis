using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int64SettingsVM : BasicSettingsVM<long>
    {
        public Int64SettingsVM(SettingsMeta memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int64SettingsVM()
            : base(SettingsMeta.Empty, default)
        {
        }

        public override long Get(JsonElement property) => property.GetInt64();

        public override long GetDefault() => default(long);

        public override SettingsNodeVM Duplicate() => new Int64SettingsVM(Meta, DefaultValue);
    }
}
