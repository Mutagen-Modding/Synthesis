using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int64SettingsNodeVM : BasicSettingsNodeVM<long>
    {
        public Int64SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int64SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override long Get(JsonElement property) => property.GetInt64();

        public override long GetDefault() => default(long);
    }
}
