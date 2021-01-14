using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class Int8SettingsNodeVM : BasicSettingsNodeVM<sbyte>
    {
        public Int8SettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public Int8SettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override sbyte Get(JsonElement property) => property.GetSByte();

        public override sbyte GetDefault() => default(sbyte);
    }
}
