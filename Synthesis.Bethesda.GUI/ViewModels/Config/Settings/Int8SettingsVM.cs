using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int8SettingsVM : BasicSettingsVM<sbyte>
    {
        public Int8SettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int8SettingsVM()
            : base(string.Empty, default)
        {
        }

        public override sbyte Get(JsonElement property) => property.GetSByte();

        public override sbyte GetDefault() => default(sbyte);
    }
}
