using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt64SettingsVM : BasicSettingsVM<ulong>
    {
        public UInt64SettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt64SettingsVM()
            : base(string.Empty, default)
        {
        }

        public override ulong Get(JsonElement property) => property.GetUInt64();

        public override ulong GetDefault() => default(ulong);
    }
}
