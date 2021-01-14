using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt8SettingsNodeVM : BasicSettingsNodeVM<byte>
    {
        public UInt8SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt8SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override byte Get(JsonElement property) => property.GetByte();

        public override byte GetDefault() => default(byte);
    }
}
