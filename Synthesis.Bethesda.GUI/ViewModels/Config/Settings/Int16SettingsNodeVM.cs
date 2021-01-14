using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int16SettingsNodeVM : BasicSettingsNodeVM<short>
    {
        public Int16SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int16SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override short Get(JsonElement property) => property.GetInt16();

        public override short GetDefault() => default(short);
    }
}
