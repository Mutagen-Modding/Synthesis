using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt8SettingsVM : BasicSettingsVM<byte>
    {
        public UInt8SettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt8SettingsVM()
            : base(string.Empty, default)
        {
        }

        public override byte Get(JsonElement property) => property.GetByte();

        public override byte GetDefault() => default(byte);
    }
}
