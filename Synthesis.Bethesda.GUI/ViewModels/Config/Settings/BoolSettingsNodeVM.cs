using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class BoolSettingsNodeVM : BasicSettingsNodeVM<bool>
    {
        public BoolSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public BoolSettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override bool Get(JsonElement property) => property.GetBoolean();

        public override bool GetDefault() => default(bool);
    }
}
