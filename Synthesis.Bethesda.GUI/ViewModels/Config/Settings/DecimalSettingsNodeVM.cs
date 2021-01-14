using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class DecimalSettingsNodeVM : BasicSettingsNodeVM<decimal>
    {
        public DecimalSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public DecimalSettingsNodeVM()
            : base(string.Empty, default)
        {
        }

        public override decimal Get(JsonElement property) => property.GetDecimal();

        public override decimal GetDefault() => default(decimal);
    }
}
