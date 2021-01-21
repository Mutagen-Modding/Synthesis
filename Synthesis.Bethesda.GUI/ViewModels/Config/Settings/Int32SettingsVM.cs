using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int32SettingsVM : BasicSettingsVM<int>
    {
        public Int32SettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int32SettingsVM()
            : base(string.Empty, default)
        {
        }

        public override int Get(JsonElement property) => property.GetInt32();

        public override int GetDefault() => default(int);

        public override SettingsNodeVM Duplicate() => new Int32SettingsVM(MemberName, DefaultValue);
    }
}
