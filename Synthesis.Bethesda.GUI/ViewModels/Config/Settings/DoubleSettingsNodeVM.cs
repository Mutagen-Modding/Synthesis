using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DoubleSettingsNodeVM : BasicSettingsNodeVM<double>
    {
        public DoubleSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public DoubleSettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override double Get(JsonElement property) => property.GetDouble();

        public override double GetDefault() => default(double);
    }
}
