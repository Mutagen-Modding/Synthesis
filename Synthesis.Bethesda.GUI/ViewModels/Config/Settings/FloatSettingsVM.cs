using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class FloatSettingsVM : BasicSettingsVM<float>
    {
        public FloatSettingsVM(MemberName memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public FloatSettingsVM()
            : base(MemberName.Empty, default)
        {
        }

        public override float Get(JsonElement property) => property.GetSingle();

        public override float GetDefault() => default(float);

        public override SettingsNodeVM Duplicate() => new FloatSettingsVM(MemberName, DefaultValue);
    }
}
