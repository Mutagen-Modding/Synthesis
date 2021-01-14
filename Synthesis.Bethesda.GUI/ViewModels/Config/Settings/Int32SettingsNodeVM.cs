using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int32SettingsNodeVM : BasicSettingsNodeVM<int>
    {
        public Int32SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int32SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override int Get(JsonElement property) => property.GetInt32();

        public override int GetDefault() => default(int);
    }
}
