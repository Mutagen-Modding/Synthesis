using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt32SettingsNodeVM : BasicSettingsNodeVM<uint>
    {
        public UInt32SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt32SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override uint Get(JsonElement property) => property.GetUInt32();

        public override uint GetDefault() => default(uint);
    }
}
