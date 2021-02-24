using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt8SettingsVM : BasicSettingsVM<byte>
    {
        public UInt8SettingsVM(MemberName memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt8SettingsVM()
            : base(MemberName.Empty, default)
        {
        }

        public override byte Get(JsonElement property) => property.GetByte();

        public override byte GetDefault() => default(byte);

        public override SettingsNodeVM Duplicate() => new UInt8SettingsVM(MemberName, DefaultValue);
    }
}
