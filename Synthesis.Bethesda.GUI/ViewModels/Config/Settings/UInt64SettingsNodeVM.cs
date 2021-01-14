using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt64SettingsNodeVM : BasicSettingsNodeVM<ulong>
    {
        public UInt64SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt64SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override ulong Get(JsonElement property) => property.GetUInt64();

        public override ulong GetDefault() => default(ulong);
    }
}
