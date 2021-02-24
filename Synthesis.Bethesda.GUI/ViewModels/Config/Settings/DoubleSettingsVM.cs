using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DoubleSettingsVM : BasicSettingsVM<double>
    {
        public DoubleSettingsVM(MemberName memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public DoubleSettingsVM()
            : base(MemberName.Empty, default)
        {
        }

        public override double Get(JsonElement property) => property.GetDouble();

        public override double GetDefault() => default(double);

        public override SettingsNodeVM Duplicate() => new DoubleSettingsVM(MemberName, DefaultValue);
    }
}
