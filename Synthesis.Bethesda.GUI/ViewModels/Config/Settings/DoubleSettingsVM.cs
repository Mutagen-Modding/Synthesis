using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DoubleSettingsVM : BasicSettingsVM<double>
    {
        public DoubleSettingsVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public DoubleSettingsVM()
            : base(string.Empty, default)
        {
        }

        public override double Get(JsonElement property) => property.GetDouble();

        public override double GetDefault() => default(double);
    }
}
