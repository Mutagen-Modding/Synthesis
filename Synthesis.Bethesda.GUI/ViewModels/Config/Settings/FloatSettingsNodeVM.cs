using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class FloatSettingsNodeVM : BasicSettingsNodeVM<float>
    {
        public FloatSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public FloatSettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override float Get(JsonElement property) => property.GetSingle();

        public override float GetDefault() => default(float);
    }
}
