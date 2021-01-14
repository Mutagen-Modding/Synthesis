using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt16SettingsNodeVM : BasicSettingsNodeVM<ushort>
    {
        public UInt16SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt16SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override ushort Get(JsonElement property) => property.GetUInt16();

        public override ushort GetDefault() => default(ushort);
    }
}
