using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt32SettingsVM : BasicSettingsVM<uint>
    {
        public UInt32SettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt32SettingsVM()
            : base(string.Empty, default)
        {
        }

        public override uint Get(JsonElement property) => property.GetUInt32();

        public override uint GetDefault() => default(uint);
    }
}
